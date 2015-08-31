/*
 * AutoMotionCaptureDevicesSelecter.cs
 * 
 *  	Developed by ほえたん(Hoetan) -- 2015/09/01
 *  	Copyright (c) 2015, ACTINIA Software. All rights reserved.
 * 		Homepage: http://actinia-software.com
 * 		E-Mail: hoetan@actinia-software.com
 * 		Twitter: https://twitter.com/hoetan3
 * 		GitHub: https://github.com/hoetan
 */

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NeuronDataReaderWraper;

using Kinect;
using Kinect2 = Windows.Kinect;
using Neuron;

using LiveAnimator;

public class AutoMotionCaptureDevicesSelecter : MonoBehaviour {

	public enum EAutoMotionCaptureDevicesSelecter
	{
		eNone,
		ePerceptionNeuron,
		eKinect1,
		eKinect2,
	}

	static public bool bDebugLog = true;

	public bool bCheckedDevices = false;
	public bool bInitDevice = false;
	public bool bInitTransform = false;
	public bool bMoveTransform = false;
	public bool bEnableAnimator = false;

    public FBXExporterForUnity sFBXExporterForUnity;
	public HumanoidAvatarLiveAnimator[] sHumanoidAvatarLiveAnimators;

    public EAutoMotionCaptureDevicesSelecter eAutoMotionCaptureDevicesSelecter = EAutoMotionCaptureDevicesSelecter.eNone;

	public GameObject prefabNeuron_DeviceJoints;
	public GameObject goNeuron_Attach;
	public Vector3 vecNeuron_Position = Vector3.zero;
	public Vector3 vecNeuron_Rotation = Vector3.zero;
	public Vector3 vecNeuron_Scale = Vector3.one;

	public GameObject prefabKinect1_DeviceJoints;
	public GameObject goKinect1_Attach;
	public Vector3 vecKinect1_Position = new Vector3(0.0f, 0.0f, 0.0f);
	public Vector3 vecKinect1_Rotation = new Vector3(0.0f, 180.0f, 0.0f);
	public Vector3 vecKinect1_Scale = Vector3.one;

	public GameObject prefabKinect2_DeviceJoints;
	public GameObject goKinect2_Attach;
	public Vector3 vecKinect2_Position = new Vector3(0.0f, 0.0f, 0.0f);
	public Vector3 vecKinect2_Rotation = new Vector3(0.0f, 0.0f, 0.0f);
	public Vector3 vecKinect2_Scale = Vector3.one;

	public string Neuron_address = "127.0.0.1";
	public int Neuron_port = 7001;
	public int Neuron_commandServerPort = 7007;
	public NeuronConnection.SocketType Neuron_socketType = NeuronConnection.SocketType.TCP;

    void Start()
	{
        // FBX Exporter for Unity (Sync Animation Custom Frame)
        if (sFBXExporterForUnity != null)
        {
            if (sFBXExporterForUnity.enabled)
            {
                sFBXExporterForUnity.bOutAnimation = false;
                sFBXExporterForUnity.bOutAnimationCustomFrame = true;
            }
        }

        // Perception Neuron
        NeuronSource source = CreateConnection(Neuron_address, Neuron_port, Neuron_commandServerPort, Neuron_socketType);
		if (source != null)
		{
			eAutoMotionCaptureDevicesSelecter = EAutoMotionCaptureDevicesSelecter.ePerceptionNeuron;
			if (source != null)
			{
				source.OnDestroy();
			}
			bCheckedDevices = true;
			if (bDebugLog) Debug.Log("Auto Devices Selecter Enabled [Perception Neuron]");
			return;
		}
		// Kinect1
		int hr = NativeMethods.NuiInitialize(NuiInitializeFlags.UsesDepthAndPlayerIndex | NuiInitializeFlags.UsesSkeleton | NuiInitializeFlags.UsesColor);
		if (hr != 0) {
		}
		else
		{
			eAutoMotionCaptureDevicesSelecter = EAutoMotionCaptureDevicesSelecter.eKinect1;
			NativeMethods.NuiShutdown();
            bCheckedDevices = true;
			if (bDebugLog) Debug.Log("Auto Devices Selecter Enabled [Kinect1]");
			return;
		}
		// Kinect2
		Kinect2.KinectSensor _Sensor = Kinect2.KinectSensor.GetDefault();
		if (_Sensor != null)
		{
			if (!_Sensor.IsOpen)
			{
				_Sensor.Open();
				_Sensor.IsAvailableChanged += (sender, evt) => {
					if (!bCheckedDevices)
					{
						if(_Sensor.IsAvailable)
						{
							eAutoMotionCaptureDevicesSelecter = EAutoMotionCaptureDevicesSelecter.eKinect2;
							if (_Sensor != null)
							{
                                if (bDebugLog) Debug.Log("[Kinect2] KinectSensor Close");
                                _Sensor.Close();
							}
						}
                        bCheckedDevices = true;
                        if (bDebugLog) Debug.Log("Auto Devices Selecter Enabled [Kinect2]");
						return;
					}
				};
			}
		}
	}

	static NeuronSource CreateConnection( string address, int port, int commandServerPort, NeuronConnection.SocketType socketType )
	{	
		NeuronSource source = null;
		IntPtr socketReference = IntPtr.Zero;
		IntPtr commandSocketReference = IntPtr.Zero;
		
		if( socketType == NeuronConnection.SocketType.TCP )
		{
			socketReference = NeuronDataReader.BRConnectTo( address, port );
			if( socketReference != IntPtr.Zero )
			{
				if (bDebugLog) Debug.Log( string.Format( "[NeuronConnection] Connected to {0}:{1}.", address, port ) );
			}
			else
			{
				if (bDebugLog) Debug.LogError( string.Format( "[NeuronConnection] Connecting to {0}:{1} failed.", address, port ) );
			}
		}
		else
		{
			socketReference = NeuronDataReader.BRStartUDPServiceAt( port );
			if( socketReference != IntPtr.Zero )
			{
				if (bDebugLog) Debug.Log( string.Format( "[NeuronConnection] Start listening at {0}.", port ) );
			}
			else
			{
				if (bDebugLog) Debug.LogError( string.Format( "[NeuronConnection] Start listening at {0} failed.", port ) );
			}
		}
		
		if( socketReference != IntPtr.Zero )
		{
			if( commandServerPort > 0 )
			{
				// connect to command server
				commandSocketReference = NeuronDataReader.BRConnectTo( address, commandServerPort );
				if( commandSocketReference != IntPtr.Zero )
				{
					if (bDebugLog) Debug.Log( string.Format( "[NeuronConnection] Connected to command server {0}:{1}.", address, commandServerPort ) );
				}
				else
				{
					if (bDebugLog) Debug.LogError( string.Format( "[NeuronConnection] Connected to command server {0}:{1} failed.", address, commandServerPort ) );
				}
			}

			source = new NeuronSource( address, port, commandServerPort, socketType, socketReference, commandSocketReference );
		}
		
		return source;
	}

	// Update is called once per frame
	void Update () {
		if (!bInitDevice) {
			if (bCheckedDevices) {
				bInitDevice = true;
				switch (eAutoMotionCaptureDevicesSelecter) {
					case EAutoMotionCaptureDevicesSelecter.ePerceptionNeuron:
						if (goNeuron_Attach == null) {
							GameObject goTopPerceptionNeuron = Instantiate(prefabNeuron_DeviceJoints) as GameObject;
							goNeuron_Attach = this.gameObject;
							goTopPerceptionNeuron.transform.parent = goNeuron_Attach.transform;
						}
						else {
							GameObject goTopPerceptionNeuron = Instantiate (prefabNeuron_DeviceJoints, goNeuron_Attach.transform.position, goNeuron_Attach.transform.rotation) as GameObject;
							goTopPerceptionNeuron.transform.localPosition = goNeuron_Attach.transform.localPosition;
							goTopPerceptionNeuron.transform.localRotation = goNeuron_Attach.transform.localRotation;
							goTopPerceptionNeuron.transform.localScale = goNeuron_Attach.transform.localScale;
							goTopPerceptionNeuron.transform.parent = goKinect1_Attach.transform;
						}
						break;
					case EAutoMotionCaptureDevicesSelecter.eKinect1:
						if (goKinect1_Attach == null) {
							GameObject goTopKinect1 = Instantiate(prefabKinect1_DeviceJoints) as GameObject;
							goKinect1_Attach = this.gameObject;
							goTopKinect1.transform.parent = goKinect1_Attach.transform;
                            Kinect1ModelControllerV2 sKinect1ModelControllerV2 = goTopKinect1.GetComponentInChildren<Kinect1ModelControllerV2>();
                            if (sKinect1ModelControllerV2 != null)
                            {
                                sKinect1ModelControllerV2.sAutoMotionCaptureDevicesSelecter = this;
                                sKinect1ModelControllerV2.sFBXExporterForUnity = sFBXExporterForUnity;
                            }
                        }
                        else {
							GameObject goTopKinect1 = Instantiate(prefabKinect1_DeviceJoints, goKinect1_Attach.transform.position, goKinect1_Attach.transform.rotation) as GameObject;
							goTopKinect1.transform.localPosition = goKinect1_Attach.transform.localPosition;
							goTopKinect1.transform.localRotation = goKinect1_Attach.transform.localRotation;
							goTopKinect1.transform.localScale = goKinect1_Attach.transform.localScale;
							goTopKinect1.transform.parent = goKinect1_Attach.transform;
                            Kinect1ModelControllerV2 sKinect1ModelControllerV2 = goTopKinect1.GetComponentInChildren<Kinect1ModelControllerV2>();
							if (sKinect1ModelControllerV2 != null)
							{
                                sKinect1ModelControllerV2.sAutoMotionCaptureDevicesSelecter = this;
                                sKinect1ModelControllerV2.sFBXExporterForUnity = sFBXExporterForUnity;
							}
						}
						break;
					case EAutoMotionCaptureDevicesSelecter.eKinect2:
						if (goKinect2_Attach == null) {
							GameObject goTopKinect2 = Instantiate(prefabKinect2_DeviceJoints) as GameObject;
							goKinect2_Attach = this.gameObject;
							goTopKinect2.transform.parent = goKinect2_Attach.transform;
                            Kinect2ModelControllerV2 sKinect2ModelControllerV2 = goTopKinect2.GetComponentInChildren<Kinect2ModelControllerV2>();
							if (sKinect2ModelControllerV2 != null)
							{
                                sKinect2ModelControllerV2.sAutoMotionCaptureDevicesSelecter = this;
                                sKinect2ModelControllerV2.sFBXExporterForUnity = sFBXExporterForUnity;
							}
						}
						else {
							GameObject goTopKinect2 = Instantiate(prefabKinect2_DeviceJoints, goKinect2_Attach.transform.position, goKinect2_Attach.transform.rotation) as GameObject;
							goTopKinect2.transform.localPosition = goKinect2_Attach.transform.localPosition;
							goTopKinect2.transform.localRotation = goKinect2_Attach.transform.localRotation;
							goTopKinect2.transform.localScale = goKinect2_Attach.transform.localScale;
							goTopKinect2.transform.parent = goKinect2_Attach.transform;
                            Kinect2ModelControllerV2 sKinect2ModelControllerV2 = goTopKinect2.GetComponentInChildren<Kinect2ModelControllerV2>();
                            if (sKinect2ModelControllerV2 != null)
                            {
                                sKinect2ModelControllerV2.sAutoMotionCaptureDevicesSelecter = this;
                                sKinect2ModelControllerV2.sFBXExporterForUnity = sFBXExporterForUnity;
                            }
                        }
                        break;
				}
			}
		}
		else {
			if (!bInitTransform)
			{
				bInitTransform = true;
				foreach (HumanoidAvatarLiveAnimator sHumanoidAvatarLiveAnimator in sHumanoidAvatarLiveAnimators)
				{
					sHumanoidAvatarLiveAnimator.bInitReady = true;
				}
				switch (eAutoMotionCaptureDevicesSelecter) {
					case EAutoMotionCaptureDevicesSelecter.ePerceptionNeuron:
						if (goNeuron_Attach != null)
						{
							goNeuron_Attach.transform.localPosition = vecNeuron_Position;
							goNeuron_Attach.transform.localRotation = Quaternion.Euler(vecNeuron_Rotation);
							goNeuron_Attach.transform.localScale = vecNeuron_Scale;
						}
						bMoveTransform = true;
						break;
					case EAutoMotionCaptureDevicesSelecter.eKinect1:
						if (goKinect1_Attach != null)
						{
							goKinect1_Attach.transform.localPosition = vecKinect1_Position;
							goKinect1_Attach.transform.localRotation = Quaternion.Euler(vecKinect1_Rotation);
							goKinect1_Attach.transform.localScale = vecKinect1_Scale;
						}
						break;
					case EAutoMotionCaptureDevicesSelecter.eKinect2:
						if (goKinect2_Attach != null)
						{
							goKinect2_Attach.transform.localPosition = vecKinect2_Position;
							goKinect2_Attach.transform.localRotation = Quaternion.Euler(vecKinect2_Rotation);
							goKinect2_Attach.transform.localScale = vecKinect2_Scale;
                        }
						break;
				}
			}
			else {
				if (!bEnableAnimator)
				{
					if (bMoveTransform)
					{
                        if (sFBXExporterForUnity != null)
                        {
                            sFBXExporterForUnity.bOutAnimation = true;
                        }
                        bEnableAnimator = true;
						foreach (HumanoidAvatarLiveAnimator sHumanoidAvatarLiveAnimator in sHumanoidAvatarLiveAnimators)
						{
							sHumanoidAvatarLiveAnimator.bEnableAnimator = true;
						}
					}
				}
			}
		}
	}
}
