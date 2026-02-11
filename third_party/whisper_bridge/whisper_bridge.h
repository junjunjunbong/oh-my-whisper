#ifndef WHISPER_BRIDGE_H
#define WHISPER_BRIDGE_H

#ifdef __cplusplus
extern "C" {
#endif

#ifdef _WIN32
#define BRIDGE_API __declspec(dllexport)
#else
#define BRIDGE_API
#endif

typedef void* bridge_handle;

/* 모델 초기화. language: "ko", "en" 등. n_threads: 0이면 자동. */
BRIDGE_API bridge_handle bridge_init(const char* model_path, const char* language, int n_threads);

/* 전사 수행. 반환값은 UTF-8 문자열 (malloc). 호출자가 bridge_string_free로 해제. */
BRIDGE_API char* bridge_transcribe(bridge_handle handle, const float* samples, int n_samples);

/* 언어 변경 */
BRIDGE_API void bridge_set_language(bridge_handle handle, const char* language);

/* 핸들 해제 */
BRIDGE_API void bridge_free(bridge_handle handle);

/* bridge_transcribe 반환 문자열 해제 */
BRIDGE_API void bridge_string_free(char* p);

#ifdef __cplusplus
}
#endif

#endif /* WHISPER_BRIDGE_H */
