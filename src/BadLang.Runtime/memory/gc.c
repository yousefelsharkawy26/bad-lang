#include <stdio.h>
#include <stdlib.h>

// BadLang Garbage Collector (Placeholder)
// Future Implementation: Mark-Sweep or Generational GC

void badlang_gc_init() {
    printf("GC: Initializing...\n");
}

void badlang_gc_collect() {
    printf("GC: Collecting garbage...\n");
}

void* badlang_gc_alloc(size_t size) {
    return malloc(size);
}
