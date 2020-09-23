using System;
using System.Runtime.InteropServices;

namespace Unity.Media
{
    public class RefHandle<T> : IDisposable
        where T : class
    {
        public bool IsCreated { get { return m_Handle.IsAllocated; } }

        public T Target
        {
            get
            {
                if (!IsCreated)
                    return null;

                return m_Handle.Target as T;
            }

            set
            {
                if (IsCreated)
                    m_Handle.Free();

                if (value != null)
                    m_Handle = GCHandle.Alloc(value, GCHandleType.Normal);
            }
        }

        GCHandle m_Handle;
        private bool Disposed = false;

        public RefHandle()
        {
        }

        public RefHandle(T target)
        {
            m_Handle = new GCHandle();
            Target = target;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
            }

            // Free any unmanaged objects here.
            if (IsCreated)
                m_Handle.Free();

            Disposed = true;
        }

        ~RefHandle()
        {
            Dispose(false);
        }
    }
}
