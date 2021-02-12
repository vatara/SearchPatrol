using Microsoft.FlightSimulator.SimConnect;
using System;
using System.Collections.Generic;
using System.Threading;

namespace SearchPatrol.Common
{
    public enum DEFINITION { Dummy = 0 }

    public enum REQUEST { Dummy = 0 }

    public class SimConnectWrapper
    {
        public static uint WM_USER_SIMCONNECT = 0x0402;

        public SimConnect simConnect = null;

        private uint currentDefinition = 1;
        private uint currentRequest = 1;

        List<SimvarRequest> requests = new List<SimvarRequest>();

        public void Connect(string name, IntPtr hWnd, WaitHandle hEventHandle, uint configIndex)
        {
            simConnect = new SimConnect(name, hWnd, WM_USER_SIMCONNECT, hEventHandle, configIndex);
        }

        public SimvarRequest CreateSimvarRequest(string name, string units, uint requestId, uint definitionId)
        {
            var request = new SimvarRequest
            {
                define = definitionId,
                request = requestId,
                name = name,
                units = units
            };
            requests.Add(request);

            currentDefinition++;
            currentRequest++;

            return request;
        }

        void RemoveSimvarRequest(SimvarRequest request)
        {
            requests.Remove(request);
        }

        bool RegisterToSimConnect(SimvarRequest request)
        {
            if (simConnect == null) return false;
            /// Define a data structure
            simConnect.AddToDataDefinition((DEFINITION)request.define, request.name, request.units, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            /// IMPORTANT: Register it with the simconnect managed wrapper marshaller
            /// If you skip this step, you will only receive a uint in the .dwData field.
            simConnect.RegisterDataDefineStruct<double>((DEFINITION)request.define);
            return true;
        }

        public void CreateAiSimulatedObject(string szContainerTitle, SIMCONNECT_DATA_INITPOSITION initPos, uint request, uint definition)
        {
            if (simConnect == null) return;
            simConnect.AICreateSimulatedObject(szContainerTitle, initPos, (REQUEST)request);
        }

        public void RequestDataOnSimObjectType(uint request, uint definition, uint radiusMeters, SIMCONNECT_SIMOBJECT_TYPE type)
        {
            if (simConnect == null) return;

            radiusMeters = Math.Min(200_000, radiusMeters);

            simConnect.RequestDataOnSimObjectType((REQUEST)request, (DEFINITION)definition, radiusMeters, type);
        }
    }
}
