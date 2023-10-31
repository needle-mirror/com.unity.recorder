using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Recorder
{
    /// <summary>
    /// A class to back up the RenderTextures onto a stack
    /// </summary>
    class RenderTextureActiveGuard : IDisposable
    {
        static Stack<RenderTexture> backups = new();

        /// <summary>
        /// Push the RenderTextures to a stack
        /// </summary>
        /// <param name="tex"></param>
        public RenderTextureActiveGuard(RenderTexture tex)
        {
            backups.Push(RenderTexture.active);
            RenderTexture.active = tex;
        }

        /// <summary>
        /// Pop the top RenderTexture from the stack and set it active
        /// </summary>
        public void Dispose()
        {
            var tex = backups.Pop();
            RenderTexture.active = tex;
        }
    }
}
