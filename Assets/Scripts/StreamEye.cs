//Joe Snider
//3/20
//
//Stream data from the HTC Vive Pro eye trackers over LSL.
//This is not very general, but it should be sufficient.
//
//Note that all the static stuff is to be compatible with the eye tracking library.

using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

using LSL;

namespace ViveSR.anipal.Eye
{
    public class StreamEye : MonoBehaviour
    {
        [Header("UI interactions")]
        public Toggle streaming;
        public Image lslStatus;
        public Text sampleRate;
        public Image eyeStatus;

        private bool eye_callback_registered = false;
        private static EyeData_v2 eyeData = new EyeData_v2();

        private liblsl.StreamInfo sinfo;
        private static liblsl.StreamOutlet soutlet;
        private string id;

        private static int channels = 9; // local time, gaze origin (x,y,z), gaze direction (yaw, pitch, roll), left pupil radius (mm), right pupil radius (mm)

        private Color stoppedColor = Color.red;
        private Color startedColor = Color.green;

        private static float lastTime = 0.0f;
        private static float nextTime = -1.0f;

        // Start is called before the first frame update
        void Start()
        {
            id = SystemInfo.deviceUniqueIdentifier;
            sinfo = new liblsl.StreamInfo("StreamEye", "eye_data", channels, 0,
                liblsl.channel_format_t.cf_float32, "eye_" + id);
            soutlet = new liblsl.StreamOutlet(sinfo);

            if (!SRanipal_Eye_Framework.Instance.EnableEye)
            {
                enabled = false;
                return;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.WORKING &&
                SRanipal_Eye_Framework.Status != SRanipal_Eye_Framework.FrameworkStatus.NOT_SUPPORT) return;

            if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == true && eye_callback_registered == false)
            {
                SRanipal_Eye_v2.WrapperRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                eye_callback_registered = true;
            }
            else if (SRanipal_Eye_Framework.Instance.EnableEyeDataCallback == false && eye_callback_registered == true)
            {
                SRanipal_Eye_v2.WrapperUnRegisterEyeDataCallback(Marshal.GetFunctionPointerForDelegate((SRanipal_Eye_v2.CallbackBasic)EyeCallback));
                eye_callback_registered = false;
            }

            if (eye_callback_registered && streaming.isOn)
            {
                pupilLeftSize = eyeData.verbose_data.left.pupil_diameter_mm;
                pupilRightSize = eyeData.verbose_data.right.pupil_diameter_mm;
                gazeDirectionNorm = eyeData.verbose_data.combined.eye_data.gaze_direction_normalized;
                gazeDirectionOrigin = eyeData.verbose_data.combined.eye_data.gaze_origin_mm;
            }

            if(streaming.isOn)
            {
                lslStatus.color = startedColor;
            } else
            {
                lslStatus.color = stoppedColor;
            }

            if(eye_callback_registered && eyeData.no_user)
            {
                eyeStatus.color = startedColor;
            } else
            {
                eyeStatus.color = stoppedColor;
            }

            sampleRate.text = "Sample rate: " + (int)(1000.0f / (nextTime - lastTime)) + " Hz";

        }

        public float pupilLeftSize = 0.0f;
        public float pupilRightSize = 0.0f;
        public Vector3 gazeDirectionNorm = new Vector3();
        public Vector3 gazeDirectionOrigin = new Vector3();

        private static float[] buffer = new float[channels];
        private static void EyeCallback(ref EyeData_v2 eye_data)
        {
            eyeData = eye_data;
            lastTime = nextTime;
            nextTime = eyeData.timestamp;
            buffer[0] = eyeData.timestamp;
            buffer[1] = eyeData.verbose_data.combined.eye_data.gaze_origin_mm.x;
            buffer[2] = eyeData.verbose_data.combined.eye_data.gaze_origin_mm.y;
            buffer[3] = eyeData.verbose_data.combined.eye_data.gaze_origin_mm.z;
            buffer[4] = eyeData.verbose_data.combined.eye_data.gaze_direction_normalized.x;
            buffer[5] = eyeData.verbose_data.combined.eye_data.gaze_direction_normalized.y;
            buffer[6] = eyeData.verbose_data.combined.eye_data.gaze_direction_normalized.z;
            buffer[7] = eyeData.verbose_data.left.pupil_diameter_mm;
            buffer[8] = eyeData.verbose_data.left.pupil_diameter_mm;
            soutlet.push_sample(buffer);
        }
    }

}
