﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NAudio.Wave;
using Microsoft.Xna.Framework.Graphics;

namespace CantStopTheBeat
{
    class SongPlayer : IWaveProvider
    {

        //reading-type stuff
        //this presumably will need different stuff to handle non-mp3 files
        //but for now, testing
        string[] files;
        int fileNum;
        Mp3FileReader bridge;
        WaveStream inputStream;

        //FFT-type stuff
        double[][] toPassToFFT;
        public bool newFFTData;
        const int fftSize = 8192;

        //output-to-player-type stuff
        const int bufferSize = 16384;  //based on being the lowest power of two above a certain length of time
                                       //still kind of magic number-y
        IWavePlayer player;
        public WaveFormat WaveFormat
        {
            get
            {
                return waveFormat;
            }
        }
        private WaveFormat waveFormat;

        //tracking-type stuff
        byte[] buffer;
        int posInBuff;
        int read;

        //other
        InterpretedTag tag;

        //??? do i need all this
        /*
        Queue<short[]> dataQueue;
        StreamWriter outstream;
        double[][] toPassToFFT;
        public bool newFFTData;
        const int fftSize = 8192;   //preferred sample size for an FFT run
         */

        bool playlistEnded = false;

        //bool ended = false;

        public SongPlayer(string[] filePaths)
        {
            files = filePaths;
            fileNum = 0;
            bridge = new Mp3FileReader(files[fileNum]);  //load up the first file in the list
            tag = new InterpretedTag(bridge.Id3v2Tag); //read its tag

            inputStream = WaveFormatConversionStream.CreatePcmStream(bridge);  //convert it to PCM

            buffer = new byte[bufferSize];

            player = new DirectSoundOut(100);
            waveFormat = inputStream.WaveFormat;
        }

        public int Read(byte[] outBuff, int offset, int count)
        {
            if(playlistEnded)
            {
                return 0;
            }
            int logged = 0;
            while (logged < count)
            {
                if (posInBuff >= read - 1)
                {
                    read = inputStream.Read(buffer, 0, buffer.Length);
                    posInBuff = 0;
                    if (read == 0)
                    {
                        nextFile();
                        if (playlistEnded)
                            break;
                    }
                }
                int length = Math.Min(count - logged, read - posInBuff);
                Array.Copy(buffer, posInBuff, outBuff, offset + logged, length);
                logged += length;

                posInBuff += length;
            }
            return logged;
        }

        private void nextFile()
        {
            //newFFTData = false;

            fileNum++;
            if (fileNum >= files.Length)
            {
                playlistEnded = true;
                return;
            }
            bridge.Close();
            bridge.Dispose();
            inputStream.Close();
            inputStream.Dispose();
            bridge = new Mp3FileReader(files[fileNum]);
            inputStream = new WaveFormatConversionStream(WaveFormat, bridge);
            tag = new InterpretedTag(bridge.Id3v2Tag);
        }

        public void start()
        {
            player.Init(this);
            player.Play();
            player.PlaybackStopped += stopped;
        }

        public void stopped(Object sender, EventArgs e)
        {
            //this does in fact happen when the playlist is concluded.
            //ended = true;
        }

        public void draw(SpriteBatch spriteBatch, SpriteFont font)
        {
            XingHeader head = bridge.XingHeader;
            if(head != null)
                spriteBatch.DrawString(font, head.ToString(), new Microsoft.Xna.Framework.Vector2(5, 5), Microsoft.Xna.Framework.Color.Black);
            else
                spriteBatch.DrawString(font, "No Xing header!", new Microsoft.Xna.Framework.Vector2(5, 5), Microsoft.Xna.Framework.Color.Black);

            if (tag.title != null)
                spriteBatch.DrawString(font, tag.title, new Microsoft.Xna.Framework.Vector2(5, 25), Microsoft.Xna.Framework.Color.Black);
            else
                spriteBatch.DrawString(font, "No title tag!", new Microsoft.Xna.Framework.Vector2(5, 25), Microsoft.Xna.Framework.Color.Black);

            if (tag.artist != null)
                spriteBatch.DrawString(font, tag.artist, new Microsoft.Xna.Framework.Vector2(5, 45), Microsoft.Xna.Framework.Color.Black);
            else
                spriteBatch.DrawString(font, "No artist tag!", new Microsoft.Xna.Framework.Vector2(5, 45), Microsoft.Xna.Framework.Color.Black);

            if (tag.album != null)
                spriteBatch.DrawString(font, tag.album, new Microsoft.Xna.Framework.Vector2(5, 65), Microsoft.Xna.Framework.Color.Black);
            else
                spriteBatch.DrawString(font, "No album tag!", new Microsoft.Xna.Framework.Vector2(5, 65), Microsoft.Xna.Framework.Color.Black);
        }

    }
}
