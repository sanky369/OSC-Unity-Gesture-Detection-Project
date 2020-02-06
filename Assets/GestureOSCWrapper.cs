using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using OSCsharp.Data;
using UniOSC;
using Gestures;

public class GestureOSCWrapper : UniOSCEventTarget
{
    [HideInInspector]
    public float px, py, pz;
    private CMobile3dGestures m_gestures;

    public override void Start()
    {
        m_gestures = GetComponent<CMobile3dGestures>();
    }
    public override void OnOSCMessageReceived(UniOSCEventArgs args)
    {
        OscMessage msg = (OscMessage)args.Packet;
        if (msg.Data.Count < 1) return;

        px = (float) msg.Data[0];
        py = (float) msg.Data[1];
        pz = (float) msg.Data[2];

        m_gestures.oscAcceleration = new Vector3(px, py, pz);
    }

}
