#pragma once

#include <cstdint>
#include <cstddef>
#include <algorithm>
#include <cassert>
#include <cstring>
#include <cstdlib>
#include <memory>

typedef uint8_t uint8;
typedef uint16_t uint16;
typedef uint32_t uint32;
typedef uint64_t uint64;
typedef int8_t int8;
typedef int16_t int16;
typedef int32_t int32;
typedef int64_t int64;

#define MDFN_FASTCALL
#define INLINE inline
#define MDFN_COLD
#define MDFN_HOT
#define NO_INLINE
#define NO_CLONE
#define MDFN_WARN_UNUSED_RESULT
#define MDFN_NOWARN_UNUSED __attribute__((unused))
#define MDFN_UNLIKELY(p) (p)
#define MDFN_LIKELY(p) (p)
//#define MDFN_ASSUME_ALIGNED(p, align) ((decltype(p))__builtin_assume_aligned((p), (align)))
#define MDFN_ASSUME_ALIGNED(p, align) (p)
#define trio_snprintf snprintf
#define trio_vprintf vprintf
#define trio_printf printf
#define trio_sprintf sprintf
#define TRUE true
#define FALSE false
#ifndef __alignas_is_defined
#define alignas(p)
#endif
#define override // remove for gcc 4.7
#define final
#define gettext_noop(s) (s)
#define MDFN_MASTERCLOCK_FIXED(n) ((int64)((double)(n) * (1LL << 32)))
static INLINE void MDFN_FastArraySet(uint32 *dst, const uint32 value, const size_t count)
{
	uint32 *const end = dst + count;
	while (dst < end)
		*dst++ = value;
}
#define _(a) (a)

typedef struct
{
	// Pitch(32-bit) must be equal to width and >= the "fb_width" specified in the MDFNGI struct for the emulated system.
	// Height must be >= to the "fb_height" specified in the MDFNGI struct for the emulated system.
	// The framebuffer pointed to by surface->pixels is written to by the system emulation code.
	uint32 *pixels;
	int pitch32;

	// Pointer to an array of int32, number of elements = fb_height, set by the driver code.  Individual elements written
	// to by system emulation code.  If the emulated system doesn't support multiple screen widths per frame, or if you handle
	// such a situation by outputting at a constant width-per-frame that is the least-common-multiple of the screen widths, then
	// you can ignore this.  If you do wish to use this, you must set all elements every frame.
	int32 *LineWidths;

	// Pointer to sound buffer, set by the driver code, that the emulation code should render sound to.
	int16 *SoundBuf;

	// Number of cycles that this frame consumed, using MDFNGI::MasterClock as a time base.
	// Set by emulation code.
	int64 MasterCycles;

	// Maximum size of the sound buffer, in frames.  Set by the driver code.
	int32 SoundBufMaxSize;

	// Number of frames currently in internal sound buffer.  Set by the system emulation code, to be read by the driver code.
	int32 SoundBufSize;

	// Set by the system emulation code every frame, to denote the horizontal and vertical offsets of the image, and the size
	// of the image.  If the emulated system sets the elements of LineWidths, then the width(w) of this structure
	// is ignored while drawing the image.
	int32 y, w, h;

	// Set(optionally) by emulation code.  If InterlaceOn is true, then assume field height is 1/2 DisplayRect.h, and
	// only every other line in surface (with the start line defined by InterlacedField) has valid data
	// (it's up to internal Mednafen code to deinterlace it).
	bool InterlaceOn;
	bool InterlaceField;

	// if true, sip rendering
	bool skip;
} EmulateSpecStruct;

#define MDFN_printf printf
#define MDFN_PrintError(...) printf

#include "endian.h"

#include "math_ops.h"

#include "../emulibc/emulibc.h"
#include "../emulibc/waterboxcore.h"

// settings
extern int Setting_HighDotclockWidth;
extern int Setting_CdSpeed;
extern int Setting_SlStart;
extern int Setting_SlEnd;

extern double Setting_ResampRateError;
extern int Setting_ResampQuality;

extern int Setting_CpuEmulation; // 0 = fast, 1 = accurate, 2 = auto
extern bool Setting_NoSpriteLimit;
extern bool Setting_AdpcmBuggy;
extern bool Setting_AdpcmNoClicks;
extern bool Setting_ChromaInterpolate;

extern int Setting_PortDevice[2];

extern bool Setting_PixelPro;
