#include <math.h>
#include <setjmp.h>
#include <stdbool.h>
#include <stdint.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>

/* ─────────────────────────────────────────────
   Garbage Collection
   ───────────────────────────────────────────── */
typedef struct GCObject {
  struct GCObject *next;
  size_t size;
  uint8_t marked;
} GCObject;

static GCObject *gc_objects_head = NULL;
static void *gc_stack_bottom = NULL;
static size_t gc_allocated_bytes = 0;
static size_t gc_threshold = 1024 * 1024; // 1MB threshold

static bool badlang_looks_like_ptr(long val);

void badlang_gc_init(void *bottom) { gc_stack_bottom = bottom; }

void badlang_gc_sweep();
void badlang_gc_collect();

void *badlang_gc_alloc(size_t size) {
  if (gc_allocated_bytes + size > gc_threshold) {
    badlang_gc_collect();
  }
  GCObject *obj = (GCObject *)malloc(sizeof(GCObject) + size);
  if (!obj) {
    fprintf(stderr, "Out of memory!");
    exit(1);
  }
  obj->size = size;
  obj->marked = 0;
  obj->next = gc_objects_head;
  gc_objects_head = obj;
  gc_allocated_bytes += size;

  // Clear memory
  memset(obj + 1, 0, size);

  return (void *)(obj + 1);
}

static GCObject *get_gc_object(void *ptr) {
  GCObject *curr = gc_objects_head;
  while (curr) {
    if ((void *)(curr + 1) == ptr) {
      return curr;
    }
    curr = curr->next;
  }
  return NULL;
}

void badlang_gc_mark(void *ptr) {
  if (!ptr)
    return;
  GCObject *obj = get_gc_object(ptr);
  if (!obj || obj->marked)
    return;

  obj->marked = 1;

  void **start = (void **)(obj + 1);
  size_t count = obj->size / sizeof(void *);
  for (size_t i = 0; i < count; i++) {
    void *potential_ptr = start[i];
    if (badlang_looks_like_ptr((long)potential_ptr)) {
      badlang_gc_mark(potential_ptr);
    }
  }
}

void badlang_gc_sweep() {
  GCObject **curr = &gc_objects_head;
  while (*curr) {
    if (!(*curr)->marked) {
      GCObject *unreached = *curr;
      *curr = unreached->next;
      gc_allocated_bytes -= unreached->size;
      free(unreached);
    } else {
      (*curr)->marked = 0;
      curr = &(*curr)->next;
    }
  }

  if (gc_allocated_bytes * 2 > 1024 * 1024) {
    gc_threshold = gc_allocated_bytes * 2;
  } else {
    gc_threshold = 1024 * 1024;
  }
}

__attribute__((noinline)) void badlang_gc_collect() {
  if (!gc_stack_bottom)
    return;

  void *stack_top = __builtin_frame_address(0);

  void **bottom = (void **)gc_stack_bottom;
  void **top = (void **)stack_top;

  if (top > bottom) {
    void **tmp = top;
    top = bottom;
    bottom = tmp;
  }

  for (void **p = top; p < bottom; p++) {
    if (badlang_looks_like_ptr((long)*p)) {
      badlang_gc_mark(*p);
    }
  }

  badlang_gc_sweep();
}

/* ─────────────────────────────────────────────
   BadString
   ───────────────────────────────────────────── */
typedef struct BadString {
  long length;
  char *data;
} BadString;

BadString *badlang_str_new(char *cstr) {
  BadString *str = (BadString *)badlang_gc_alloc(sizeof(BadString));
  if (!str)
    return NULL;
  str->length = strlen(cstr);
  str->data = (char *)badlang_gc_alloc(str->length + 1);
  if (!str->data) {
    return NULL;
  }
  strcpy(str->data, cstr);
  return str;
}

BadString *badlang_str_concat(BadString *a, BadString *b) {
  if (!a && !b)
    return badlang_str_new("");
  if (!a)
    return b;
  if (!b)
    return a;
  BadString *str = (BadString *)badlang_gc_alloc(sizeof(BadString));
  if (!str)
    return NULL;
  str->length = a->length + b->length;
  str->data = (char *)badlang_gc_alloc(str->length + 1);
  if (!str->data) {
    return NULL;
  }
  strcpy(str->data, a->data);
  strcat(str->data, b->data);
  return str;
}

void badlang_str_print(BadString *s) {
  if (s && s->data)
    printf("%s", s->data);
}

void badlang_str_println(BadString *s) {
  if (s && s->data)
    printf("%s\n", s->data);
  else
    printf("\n");
}

/* Print a dynamically-typed value (i64 that may be a double-bits or a
 * BadString*) */
static bool badlang_looks_like_ptr(long val); /* forward decl */
void badlang_print_value(long val) {
  /* Try to interpret as a string pointer first, but only if it looks like a
   * valid heap pointer */
  if (badlang_looks_like_ptr(val)) {
    BadString *s = (BadString *)val;
    if (s->data != NULL && s->length >= 0 && s->length < 1000000) {
      printf("%s", s->data);
      return;
    }
  }
  /* Otherwise, interpret as a bitwise-encoded double */
  double d;
  memcpy(&d, &val, sizeof(double));
  printf("%g", d);
}

BadString *badlang_num_to_str(double n) {
  char buf[64];
  snprintf(buf, sizeof(buf), "%g", n);
  return badlang_str_new(buf);
}

bool badlang_str_eq(BadString *a, BadString *b) {
  if (!a || !b)
    return a == b;
  if (a->length != b->length)
    return false;
  return memcmp(a->data, b->data, a->length) == 0;
}

long badlang_str_length(BadString *s) { return s ? s->length : 0; }

BadString *badlang_str_at(BadString *s, long idx) {
  if (!s || idx < 0 || idx >= s->length)
    return badlang_str_new("");
  char buf[2] = {s->data[idx], '\0'};
  return badlang_str_new(buf);
}

/* ─────────────────────────────────────────────
   BadClosure
   ───────────────────────────────────────────── */
typedef struct BadClosure {
  void *FunctionPtr;
  int CaptureCount;
  void *Captures[]; /* Flexible array member */
} BadClosure;

void *badlang_closure_alloc(void *func_ptr, int capture_count) {
  size_t size = sizeof(BadClosure) + (capture_count * sizeof(void *));
  BadClosure *closure = (BadClosure *)badlang_gc_alloc(size);
  if (!closure)
    return NULL;
  closure->FunctionPtr = func_ptr;
  closure->CaptureCount = capture_count;
  /* Captures will be populated by LLVM code */
  return closure;
}

/* ─────────────────────────────────────────────
   Exception Support  (setjmp/longjmp based)
   ───────────────────────────────────────────── */

/* Boxed exception value */
typedef struct BadException {
  BadString *message;
  double code;
} BadException;

/* Exception jump-buffer stack (up to 64 nested try blocks) */
#define BADLANG_MAX_TRY_DEPTH 64
static jmp_buf _badlang_jmp_bufs[BADLANG_MAX_TRY_DEPTH];
static BadException *_badlang_pending_ex[BADLANG_MAX_TRY_DEPTH];
static int _badlang_try_depth = 0;

/* Called by compiled try-block preamble: push a new jmp_buf */
int *badlang_try_begin() {
  if (_badlang_try_depth >= BADLANG_MAX_TRY_DEPTH)
    return NULL;
  _badlang_pending_ex[_badlang_try_depth] = NULL;
  return (int *)_badlang_jmp_bufs[_badlang_try_depth++];
}

/* Called after the try body completes normally */
void badlang_try_end() {
  if (_badlang_try_depth > 0)
    _badlang_try_depth--;
}

/* Retrieve the exception caught in the current catch block */
BadException *badlang_current_exception() {
  int depth = _badlang_try_depth;
  return (depth >= 0 && depth < BADLANG_MAX_TRY_DEPTH)
             ? _badlang_pending_ex[depth]
             : NULL;
}

BadException *badlang_exception_new(BadString *msg) {
  BadException *ex = (BadException *)badlang_gc_alloc(sizeof(BadException));
  ex->message = msg ? msg : badlang_str_new("Unknown error");
  ex->code = 0;
  return ex;
}

BadString *badlang_exception_message(BadException *ex) {
  return ex ? ex->message : badlang_str_new("");
}

/* Throw: unwind to the nearest enclosing try via longjmp */
void badlang_throw(BadException *ex) {
  if (_badlang_try_depth > 0) {
    int depth = --_badlang_try_depth;
    _badlang_pending_ex[depth] = ex;
    /* Jump to the catch handler registered by badlang_try_begin */
    longjmp(_badlang_jmp_bufs[depth], 1);
  }
  /* Unhandled exception: print and abort */
  fprintf(stderr, "Unhandled exception: %s\n",
          ex && ex->message ? ex->message->data : "(unknown)");
  exit(1);
}

/* Dummy personality function stub (LLVM IR may reference it) */
int __gxx_personality_v0() { return 0; }

/* ─────────────────────────────────────────────
   Math StdLib
   ───────────────────────────────────────────── */
double badlang_math_abs(double x) { return fabs(x); }
double badlang_math_sqrt(double x) { return sqrt(x); }
double badlang_math_pow(double b, double e) { return pow(b, e); }
double badlang_math_floor(double x) { return floor(x); }
double badlang_math_ceil(double x) { return ceil(x); }
double badlang_math_round(double x) { return round(x); }
double badlang_math_log(double x) { return log(x); }
double badlang_math_log2(double x) { return log2(x); }
double badlang_math_sin(double x) { return sin(x); }
double badlang_math_cos(double x) { return cos(x); }
double badlang_math_tan(double x) { return tan(x); }
double badlang_math_min(double a, double b) { return a < b ? a : b; }
double badlang_math_max(double a, double b) { return a > b ? a : b; }
/* pi constant accessor */
double badlang_math_pi() { return M_PI; }

/* ─────────────────────────────────────────────
   List StdLib (dynamic array of i64 slots)
   ───────────────────────────────────────────── */
typedef struct BadList {
  long length;
  long capacity;
  long *data; /* stores raw i64 values (pointers or numbers) */
} BadList;

BadList *badlang_list_new() {
  BadList *list = (BadList *)badlang_gc_alloc(sizeof(BadList));
  list->length = 0;
  list->capacity = 8;
  list->data = (long *)badlang_gc_alloc(sizeof(long) * list->capacity);
  return list;
}

void badlang_list_push(BadList *list, long value) {
  if (list->length == list->capacity) {
    long old_capacity = list->capacity;
    list->capacity *= 2;
    long *new_data = (long *)badlang_gc_alloc(sizeof(long) * list->capacity);
    memcpy(new_data, list->data, sizeof(long) * old_capacity);
    list->data = new_data;
  }
  list->data[list->length++] = value;
}

long badlang_list_get(BadList *list, long idx) {
  if (!list || idx < 0 || idx >= list->length)
    return 0;
  return list->data[idx];
}

void badlang_list_set(BadList *list, long idx, long value) {
  if (list && idx >= 0 && idx < list->length)
    list->data[idx] = value;
}

long badlang_list_length(BadList *list) { return list ? list->length : 0; }

long badlang_list_pop(BadList *list) {
  if (!list || list->length == 0)
    return 0;
  return list->data[--list->length];
}

/* ─────────────────────────────────────────────
   File StdLib
   ───────────────────────────────────────────── */
typedef struct BadFile {
  FILE *handle;
  bool is_open;
} BadFile;

BadFile *badlang_file_open(BadString *path, BadString *mode) {
  BadFile *f = (BadFile *)badlang_gc_alloc(sizeof(BadFile));
  f->handle = fopen(path->data, mode ? mode->data : "r");
  f->is_open = f->handle != NULL;
  return f;
}

void badlang_file_close(BadFile *f) {
  if (f && f->handle) {
    fclose(f->handle);
    f->is_open = false;
  }
}

BadString *badlang_file_read_line(BadFile *f) {
  if (!f || !f->handle)
    return badlang_str_new("");
  char buf[4096];
  if (!fgets(buf, sizeof(buf), f->handle))
    return badlang_str_new("");
  /* Strip trailing newline */
  size_t len = strlen(buf);
  if (len > 0 && buf[len - 1] == '\n')
    buf[len - 1] = '\0';
  return badlang_str_new(buf);
}

long badlang_file_write(BadFile *f, BadString *s) {
  if (!f || !f->handle || !s)
    return 0;
  return (long)fputs(s->data, f->handle);
}

bool badlang_file_is_eof(BadFile *f) {
  return !f || !f->handle || feof(f->handle);
}

long badlang_console_read_line(BadString *prompt) {
  if (prompt && prompt->data) {
    printf("%s", prompt->data);
    fflush(stdout);
  }
  char buf[4096];
  if (!fgets(buf, sizeof(buf), stdin))
    return (long)badlang_str_new("");
  /* Strip trailing newline */
  size_t len = strlen(buf);
  if (len > 0 && buf[len - 1] == '\n')
    buf[len - 1] = '\0';
  return (long)badlang_str_new(buf);
}

/* ─────────────────────────────────────────────
   Map StdLib (Simple O(n) implementation)
   ───────────────────────────────────────────── */
typedef struct BadMap {
  BadList *keys;
  BadList *values;
} BadMap;

BadMap *badlang_map_new() {
  BadMap *map = (BadMap *)badlang_gc_alloc(sizeof(BadMap));
  map->keys = badlang_list_new();
  map->values = badlang_list_new();
  return map;
}

bool badlang_val_eq(long a, long b); // Forward decl

void badlang_map_set(BadMap *map, long key, long value) {
  if (!map)
    return;
  for (long i = 0; i < map->keys->length; i++) {
    if (badlang_val_eq(map->keys->data[i], key)) {
      map->values->data[i] = value;
      return;
    }
  }
  badlang_list_push(map->keys, key);
  badlang_list_push(map->values, value);
}

long badlang_map_get(BadMap *map, long key) {
  if (!map)
    return 0;
  for (long i = 0; i < map->keys->length; i++) {
    if (badlang_val_eq(map->keys->data[i], key)) {
      return map->values->data[i];
    }
  }
  return 0;
}

long badlang_map_size(BadMap *map) { return map ? map->keys->length : 0; }

long badlang_map_key_at(BadMap *map, long idx) {
  if (!map || !map->keys || idx < 0 || idx >= map->keys->length)
    return 0;
  return map->keys->data[idx];
}

/* Helper: check if an i64 value looks like a heap pointer (not a double bit
 * pattern) */
static bool badlang_looks_like_ptr(long val) {
  /* On x86-64, heap pointers are typically:
     - Non-zero
     - Aligned to at least 8 bytes
     - In the user-space address range (below 0x00007FFFFFFFFFFF)
     - Above the first page (> 0x1000)
     Double-encoded i64s for normal numbers (e.g., 500.0 = 0x4078800000000000)
     often look like very large addresses or have specific patterns. */
  if (val == 0)
    return false;
  unsigned long uval = (unsigned long)val;
  /* Must be 8-byte aligned (badlang_gc_alloc always returns aligned memory) */
  if (uval & 0x7)
    return false;
  /* Must be in reasonable user-space range: above 4096, below 2^48 */
  if (uval < 0x1000)
    return false;
  if (uval > 0x0000FFFFFFFFFFFF)
    return false;
  return true;
}

/* Helper for equality — attempts deep string comparison when both values look
 * like pointers */
bool badlang_val_eq(long a, long b) {
  if (a == b)
    return true;
  /* Only attempt deep comparison when both look like heap pointers */
  if (badlang_looks_like_ptr(a) && badlang_looks_like_ptr(b)) {
    BadString *sa = (BadString *)a;
    BadString *sb = (BadString *)b;
    if (sa->data != NULL && sb->data != NULL && sa->length >= 0 &&
        sb->length >= 0 && sa->length < 1000000 && sb->length < 1000000) {
      return badlang_str_eq(sa, sb);
    }
  }
  return false;
}
