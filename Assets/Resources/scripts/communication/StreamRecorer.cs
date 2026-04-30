using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DSUnityViz
{
    public class StreamRecorder
    {
        /// <summary>
        /// Recording mode, 
        /// </summary>
        RECORDING_TYPE recording_mode = RECORDING_TYPE.NOTHING;
        string recording_file_name = "";

        System.IO.TextReader reader;
        System.IO.TextWriter writer;

        TimeSpan[] read_timing_of_data;
        string[] read_data;

        /// <summary>
        /// Timer 
        /// </summary>
        System.Diagnostics.Stopwatch stp = new System.Diagnostics.Stopwatch();

        public enum RECORDING_TYPE
        {
            NOTHING = 0,
            RECORD,
            REPLAY,
        };

        public StreamRecorder()
        {
            Init();
        }

        public void Init()
        {
            recording_mode = RECORDING_TYPE.NOTHING;
            recording_file_name = "";

            if (reader != null) { reader.Close(); reader = null; }
            if (writer != null) { writer.Close(); writer = null; }

            stp.Stop(); stp.Reset();
        }
        
        public void SetReplayMode(RECORDING_TYPE mode, string filename)
        {
            switch (mode)
            {
                default:
                case RECORDING_TYPE.NOTHING:
                    break;
                case RECORDING_TYPE.RECORD:
                    recording_mode = RECORDING_TYPE.RECORD;
                    break;
                case RECORDING_TYPE.REPLAY:
                    recording_mode = RECORDING_TYPE.REPLAY;
                    break;
            }
            recording_file_name = filename;
        }

        private void startRecording(string[] meta_data)
        {
            writer = new System.IO.StreamWriter(recording_file_name);

            writer.WriteLine("Meta[Type]:DSUnityViz binary record data");
            writer.WriteLine("Meta[Version]:0.0.0.1");
            foreach (var x in meta_data)
            {
                writer.WriteLine("Meta" + x);
            }

            stp.Start();
        }

        private void stopRecording()
        {
            stp.Stop();
            writer.Close();
        }

        private void loadReplayData()
        {
            using (reader = new System.IO.StreamReader(recording_file_name))
            {
                string line_text;

                List<TimeSpan> tss = new List<TimeSpan>();
                List<string> datas = new List<string>();

                while ((line_text = reader.ReadLine()) != null)
                {
                    if (line_text.StartsWith("Meta")) continue;
                    if (line_text.StartsWith("Data"))
                    {
                        const int TimeStartPos = 5;
                        const int DataStartPos = 9;
                        string intm = line_text.Substring(TimeStartPos, 8);
                        TimeSpan ts = TimeSpan.Parse(intm);
                        string data_str = line_text.Substring(DataStartPos);

                        tss.Add(ts);
                        datas.Add(data_str);
                    }
                }
                read_timing_of_data = tss.ToArray();
                read_data = datas.ToArray();
            }

            stp.Start();
        }

        public bool IsRecording()
        { return recording_mode == RECORDING_TYPE.RECORD; }

        public bool IsPlaying()
        { return recording_mode == RECORDING_TYPE.REPLAY; }

        public void Record(byte[] data)
        {
            string time_string = stp.Elapsed.Duration().ToString();

            string data_string_base64 =
                Convert.ToBase64String(data);

            writer.Write("Data[" + time_string + "]:");
            writer.Write(data_string_base64);
        }

        public byte[] Replay()
        {
            return Replay(stp.Elapsed.Duration());
        }

        public byte[] Replay(TimeSpan ts)
        {
            int index = Array.FindLastIndex(read_timing_of_data, o => o.Duration() < ts);
            byte[] ret = Convert.FromBase64String(read_data[index]);
            return ret;
        }



    }
}
