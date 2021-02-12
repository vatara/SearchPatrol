using SearchPatrol.Common;
using System;

namespace SearchPatrol.Wpf
{
    public class SimvarRequestObservable : ObservableObject
    {
        public SimvarRequest simvarRequest = new SimvarRequest();

        public DEFINITION eDef
        {
            get => (DEFINITION)simvarRequest.define;
            //set => simvarRequest.eDef = value;
        }
        public REQUEST eRequest
        {
            get => (REQUEST)simvarRequest.request;
            set => simvarRequest.request = (uint)value;
        }

        public string sName
        {
            get => simvarRequest.name;
            set => simvarRequest.name = value;
        }

        public double dValue
        {
            get => simvarRequest.value;
            set => SetProperty(ref simvarRequest.value, value);
        }

        public string sUnits {
            get => simvarRequest.units;
            set => simvarRequest.units = value;
        }

        public bool bPending = true;

        public bool bStillPending
        {
            get => simvarRequest.m_bStillPending;
            set => SetProperty(ref simvarRequest.m_bStillPending, value);
        }
    }
}
