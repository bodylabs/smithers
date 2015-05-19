﻿// Copyright (c) 2014, Body Labs, Inc.
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without
// modification, are permitted provided that the following conditions
// are met:
//
// 1. Redistributions of source code must retain the above copyright
// notice, this list of conditions and the following disclaimer.
//
// 2. Redistributions in binary form must reproduce the above copyright
// notice, this list of conditions and the following disclaimer in the
// documentation and/or other materials provided with the distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
// FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
// COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
// INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING,
// BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS
// OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED
// AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT
// LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY
// WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
// POSSIBILITY OF SUCH DAMAGE.

using Smithers.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Smithers.Sessions
{
    public class Session<TMetadata, TShot, TShotDefinition, TSavedItem>
        where TShotDefinition : ShotDefinition
        where TSavedItem : SavedItem
        where TShot : Shot<TShotDefinition, TSavedItem>, new()
        where TMetadata : class
    {
        public static readonly string METADATA_FILE = "Info.json";

        /// <summary>
        /// Create a brand new capture session with new metadata.
        /// </summary>
        public Session(string sessionPath, IEnumerable<TShotDefinition> shotDefinitions)
        {
            this.SessionPath = sessionPath;

            this.Shots = new List<TShot>();
            foreach (TShotDefinition shotDefinition in shotDefinitions)
                this.AddShot(shotDefinition);
        }

        public virtual void AddShot(TShotDefinition shotDefinition)
        {
            this.Shots.Add(new TShot() { ShotDefinition = shotDefinition });
        }

        public string SessionPath { get; private set; }

        public virtual TMetadata GetMetadata() { return null; }

        public List<TShot> Shots { get; set; }

        public int MaximumFrameCount
        {
            get
            {
                if (this.Shots.Count == 0)
                    return ShotDefinition.DEFAULT.MaximumFrameCount;
                else
                    return this.Shots.Max(x => x.ShotDefinition.MaximumFrameCount);
            }
        }
    }

    /// <summary>
    /// Placeholder base class, available for subclassing if needed.
    /// </summary>
    public class ShotDefinition {
        public static readonly ShotDefinition DEFAULT = new ShotDefinition();

        /// <summary>
        /// Shot duration in milliseconds. (This is like an inverse of shutter speed.)
        /// </summary>
        public virtual int ShotDuration { get { return 100; } }

        /// <summary>
        /// Maximum frames which will be recorded. If more frames arrive during
        /// ShotDuration, subsequent frames will be discarded.
        /// </summary>
        public virtual int MaximumFrameCount { get { return 10; } }
    }

    public class Shot<TShotDefinition, TSavedItem>
        where TShotDefinition : ShotDefinition
        where TSavedItem : SavedItem
    {
        public Shot() {
            this.SavedItems = new List<TSavedItem>();
        }

        public Shot(TShotDefinition shotDefinition) : this()
        {
            this.ShotDefinition = shotDefinition;
        }

        public TShotDefinition ShotDefinition { get; set; }

        public bool Completed { get; set; }

        public List<TSavedItem> SavedItems { get; set; }
    }

    public class SavedItem
    {
        public SavedItemType Type { get; set; }
        public TimeSpan? Timestamp { get; set; }

        /// <summary>
        /// Since these Path values are serialized with the session, and eventually
        /// deserialized on a different system, these paths are relative to SessionPath.
        /// </summary>
        public string Path { get; set; }
    }
}
