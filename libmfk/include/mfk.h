/**
 * @file mfk.h
 * @author MXPSQL Server 20953 Onetechguy (2000onetechguy@gmail.com)
 * @brief Library for working with MFK/FRACTRAN.
 * @version 0.1
 * @date 2026-05-11
 * 
 * @copyright Copyright (c) 2026
 * 
 */

#ifndef MXPSQL_MFK_H
#define MXPSQL_MFK_H

#include <stdlib.h>
#include <stddef.h>
#include <stdint.h>
#include <stdbool.h>

#ifdef _WIN32
  #ifdef MFK_BUILD
    #define MFK_API __declspec(dllexport)
  #else
    #define MFK_API __declspec(dllimport)
  #endif
#else
  #define MFK_API
#endif

#ifdef __cplusplus
extern "C" {
#endif

/* Start of Portability Arena APIs */

/**
 * @brief Methods for the Portability Arena.
 * 
 */
typedef struct mfk_parena_mth_t {
    mfk_bigint_t (*bigint_new)(mfk_parena_t*);
    mfk_bigint_t (*bigint_set)(mfk_parena_t*, mfk_bigint_t);
    mfk_bigint_t (*bigint_add)(mfk_parena_t*, mfk_bigint_t, mfk_bigint_t);
    mfk_bigint_t (*bigint_sub)(mfk_parena_t*, mfk_bigint_t, mfk_bigint_t);
    mfk_bigint_t (*bigint_mult)(mfk_parena_t*, mfk_bigint_t, mfk_bigint_t);
    mfk_bigint_t (*bigint_divrem)(mfk_parena_t*, mfk_bigint_t, mfk_bigint_t, mfk_bigint_t*);
} mfk_parena_mth_t;
/**
 * @brief The Portability Arena Object.
 * 
 */
typedef struct mfk_parena_t mfk_parena_t;

mfk_parena_t* mfk_parena_default();
mfk_parena_t* mfk_parena_new(mfk_parena_mth_t* mth);
void mfk_parena_delete(mfk_parena_t* parena);
void mfk_parena_mths(mfk_parena_t* parena, mfk_parena_mth_t* mth);

/* End of Portability/Arena APIs */

/* Start of Numerical APIs */

/**
 * @brief Unsafe type representing a bigint.
 * 
 * The internals of this type is handled, defined, and owned by the backend.  
 * Users must treat this as an opaque type.
 */
typedef void* mfk_bigint_t;
/**
 * @brief Type representing a fraction.
 * 
 */
typedef struct mfk_frac_t mfk_frac_t;

mfk_frac_t* mfk_frac_new(mfk_parena_t* parena);
mfk_frac_t* mfk_frac_new2(mfk_parena_t* parena, mfk_bigint_t num, mfk_bigint_t den);
void mfk_frac_destroy(mfk_frac_t* frac);

/* End of Numerical APIs */



/* Start of Interpreter API */

/**
 * @brief The MFK interpreter.
 * 
 */
typedef struct mfk_interp_t mfk_interp_t;

/* End of Interpreter API */


/* Start of Churner API */

/**
 * @brief The MFK lexer/parser.
 * 
 */
typedef struct mfk_churner_t mfk_churner_t;

/* End of Churner API */



/* Start of Assembler API */

/**
 * @brief The MFK Assembler.
 * 
 * This type is dedicated to allow the programmatic construction of MFK programs.
 */
typedef struct mfk_asm_t mfk_asm_t;

mfk_asm_t* mfk_asm_new(mfk_parena_t* parena);
void mfk_asm_destroy(mfk_asm_t* assembler);

bool mfk_set_N(mfk_asm_t* assembler, const mfk_bigint_t N);
bool mfk_set_maxiter(mfk_asm_t* assembler, const mfk_bigint_t i);
bool mfk_set_seed(mfk_asm_t* assembler, const mfk_bigint_t seed);

bool mfk_append_frac(mfk_asm_t* assembler, const mfk_frac_t* frac);

/* End of Assembler API */


#ifdef __cplusplus
}
#endif

#endif
