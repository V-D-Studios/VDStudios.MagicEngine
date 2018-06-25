﻿namespace Veldrid.SPIRV
{
    /// <summary>
    /// The output of a cross-compile operation of a compute shader from SPIR-V to some target language.
    /// </summary>
    public class ComputeCompilationResult
    {
        /// <summary>
        /// The translated shader source code.
        /// </summary>
        public string ComputeShader { get; }

        internal ComputeCompilationResult(string computeCode)
        {
            ComputeShader = computeCode;
        }
    }

}
