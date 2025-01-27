﻿using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

namespace FFmpegUnityBind2.Components
{
    static class WAV
    {
        const int HEADER_SIZE = 44;

        public static void Save(string path, AudioClip clip)
        {
            // Make sure directory exists if user is saving to sub dir.
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            using (var fileStream = CreateEmpty(path))
            {
                ConvertAndWrite(fileStream, clip);

                WriteHeader(fileStream, clip);
            }
        }

        public static AudioClip TrimSilence(AudioClip clip, float min)
        {
            var samples = new float[clip.samples];

            clip.GetData(samples, 0);

            return TrimSilence(new List<float>(samples), min, clip.channels, clip.frequency);
        }

        public static AudioClip TrimSilence(List<float> samples, float min, int channels, int hz)
        {
            int i;

            for (i = 0; i < samples.Count; i++)
            {
                if (Mathf.Abs(samples[i]) > min)
                {
                    break;
                }
            }

            samples.RemoveRange(0, i);

            for (i = samples.Count - 1; i > 0; i--)
            {
                if (Mathf.Abs(samples[i]) > min)
                {
                    break;
                }
            }

            samples.RemoveRange(i, samples.Count - i);

            var clip = AudioClip.Create("TempClip", samples.Count, channels, hz, false);

            clip.SetData(samples.ToArray(), 0);

            return clip;
        }

        static FileStream CreateEmpty(string filepath)
        {
            var fileStream = new FileStream(filepath, FileMode.Create);
            byte emptyByte = new byte();

            for (int i = 0; i < HEADER_SIZE; i++) //preparing the header
            {
                fileStream.WriteByte(emptyByte);
            }

            return fileStream;
        }

        static void ConvertAndWrite(FileStream fileStream, AudioClip clip)
        {
            var samples = new float[clip.samples];

            clip.GetData(samples, 0);

            short[] intData = new short[samples.Length];
            //converting in 2 float[] steps to Int16[], //then Int16[] to Byte[]

            byte[] bytesData = new byte[samples.Length * 2];
            //bytesData array is twice the size of
            //dataSource array because a float converted in Int16 is 2 bytes.

            int rescaleFactor = 32767; //to convert float to Int16

            for (int i = 0; i < samples.Length; i++)
            {
                intData[i] = (short)(samples[i] * rescaleFactor);
                byte[] byteArr = BitConverter.GetBytes(intData[i]);
                byteArr.CopyTo(bytesData, i * 2);
            }

            fileStream.Write(bytesData, 0, bytesData.Length);
        }

        static void WriteHeader(FileStream fileStream, AudioClip clip)
        {
            var hz = clip.frequency;
            var channels = clip.channels;
            var samples = clip.samples;

            fileStream.Seek(0, SeekOrigin.Begin);

            byte[] riff = System.Text.Encoding.UTF8.GetBytes("RIFF");
            fileStream.Write(riff, 0, 4);

            byte[] chunkSize = BitConverter.GetBytes(fileStream.Length - 8);
            fileStream.Write(chunkSize, 0, 4);

            byte[] wave = System.Text.Encoding.UTF8.GetBytes("WAVE");
            fileStream.Write(wave, 0, 4);

            byte[] fmt = System.Text.Encoding.UTF8.GetBytes("fmt ");
            fileStream.Write(fmt, 0, 4);

            byte[] subChunk1 = BitConverter.GetBytes(16);
            fileStream.Write(subChunk1, 0, 4);

            //UInt16 two = 2;
            ushort one = 1;

            byte[] audioFormat = BitConverter.GetBytes(one);
            fileStream.Write(audioFormat, 0, 2);

            byte[] numChannels = BitConverter.GetBytes(channels);
            fileStream.Write(numChannels, 0, 2);

            byte[] sampleRate = BitConverter.GetBytes(hz);
            fileStream.Write(sampleRate, 0, 4);

            byte[] byteRate = BitConverter.GetBytes(hz * channels * 2); // sampleRate * bytesPerSample*number of channels, here 44100*2*2
            fileStream.Write(byteRate, 0, 4);

            ushort blockAlign = (ushort)(channels * 2);
            fileStream.Write(BitConverter.GetBytes(blockAlign), 0, 2);

            ushort bps = 16;
            byte[] bitsPerSample = BitConverter.GetBytes(bps);
            fileStream.Write(bitsPerSample, 0, 2);

            byte[] datastring = System.Text.Encoding.UTF8.GetBytes("data");
            fileStream.Write(datastring, 0, 4);

            byte[] subChunk2 = BitConverter.GetBytes(samples * channels * 2);
            fileStream.Write(subChunk2, 0, 4);
        }
    }
}