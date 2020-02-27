#ifdef UNITY_SHADER_VARIABLES_MATRIX_DEFS_HDCAMERA_INCLUDED
    #error Mixing HDCamera and legacy Unity matrix definitions
#endif

#ifndef UNITY_SHADER_VARIABLES_MATRIX_DEFS_LEGACY_UNITY_INCLUDED
#define UNITY_SHADER_VARIABLES_MATRIX_DEFS_LEGACY_UNITY_INCLUDED

#define UNITY_MATRIX_M     ApplyCameraTranslationToMatrix(unity_ObjectToWorld)
#define UNITY_MATRIX_I_M   ApplyCameraTranslationToInverseMatrix(unity_WorldToObject)
#define UNITY_MATRIX_V     unity_MatrixV
#define UNITY_MATRIX_I_V   unity_MatrixInvV
#define UNITY_MATRIX_P     OptimizeProjectionMatrix(glstate_matrix_projection)
#define UNITY_MATRIX_I_P   ERROR_UNITY_MATRIX_I_P_IS_NOT_DEFINED
#define UNITY_MATRIX_VP    unity_MatrixVP
#define UNITY_MATRIX_I_VP  ERROR_UNITY_MATRIX_I_VP_IS_NOT_DEFINED

#endif // UNITY_SHADER_VARIABLES_MATRIX_DEFS_LEGACY_UNITY_INCLUDED
