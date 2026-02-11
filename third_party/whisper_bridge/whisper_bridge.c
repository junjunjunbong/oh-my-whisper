#include "whisper_bridge.h"
#include "whisper.h"
#include <stdlib.h>
#include <string.h>
#include <stdio.h>

typedef struct {
    struct whisper_context* ctx;
    char language[16];
    int n_threads;
} bridge_ctx;

BRIDGE_API bridge_handle bridge_init(const char* model_path, const char* language, int n_threads) {
    struct whisper_context_params cparams = whisper_context_default_params();
    struct whisper_context* ctx = whisper_init_from_file_with_params(model_path, cparams);
    if (!ctx) {
        return NULL;
    }

    bridge_ctx* b = (bridge_ctx*)calloc(1, sizeof(bridge_ctx));
    b->ctx = ctx;
    b->n_threads = (n_threads > 0) ? n_threads : 4;

    if (language && language[0]) {
        strncpy(b->language, language, sizeof(b->language) - 1);
    } else {
        strncpy(b->language, "ko", sizeof(b->language) - 1);
    }

    return (bridge_handle)b;
}

BRIDGE_API char* bridge_transcribe(bridge_handle handle, const float* samples, int n_samples) {
    if (!handle || !samples || n_samples <= 0) {
        char* empty = (char*)malloc(1);
        empty[0] = '\0';
        return empty;
    }

    bridge_ctx* b = (bridge_ctx*)handle;

    struct whisper_full_params params = whisper_full_default_params(WHISPER_SAMPLING_GREEDY);
    params.language = b->language;
    params.n_threads = b->n_threads;
    params.translate = false;
    params.no_context = true;
    params.single_segment = false;
    params.print_special = false;
    params.print_progress = false;
    params.print_realtime = false;
    params.print_timestamps = false;

    int ret = whisper_full(b->ctx, params, samples, n_samples);
    if (ret != 0) {
        char* empty = (char*)malloc(1);
        empty[0] = '\0';
        return empty;
    }

    int n_segments = whisper_full_n_segments(b->ctx);

    /* 전체 세그먼트 텍스트 길이 계산 */
    size_t total_len = 0;
    for (int i = 0; i < n_segments; i++) {
        const char* text = whisper_full_get_segment_text(b->ctx, i);
        if (text) {
            total_len += strlen(text);
        }
    }

    char* result = (char*)malloc(total_len + 1);
    result[0] = '\0';

    for (int i = 0; i < n_segments; i++) {
        const char* text = whisper_full_get_segment_text(b->ctx, i);
        if (text) {
            strcat(result, text);
        }
    }

    return result;
}

BRIDGE_API void bridge_set_language(bridge_handle handle, const char* language) {
    if (!handle || !language) return;
    bridge_ctx* b = (bridge_ctx*)handle;
    strncpy(b->language, language, sizeof(b->language) - 1);
    b->language[sizeof(b->language) - 1] = '\0';
}

BRIDGE_API void bridge_free(bridge_handle handle) {
    if (!handle) return;
    bridge_ctx* b = (bridge_ctx*)handle;
    if (b->ctx) {
        whisper_free(b->ctx);
    }
    free(b);
}

BRIDGE_API void bridge_string_free(char* p) {
    if (p) free(p);
}
