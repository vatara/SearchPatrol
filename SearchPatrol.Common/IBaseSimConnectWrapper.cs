using System;

namespace SearchPatrol.Common
{
    public interface IBaseSimConnectWrapper
    {
        uint GetUserSimConnectWinEvent();
        void ReceiveSimConnectMessage();
        void SetWindowHandle(IntPtr _hWnd);
        void Disconnect();
    }
}
