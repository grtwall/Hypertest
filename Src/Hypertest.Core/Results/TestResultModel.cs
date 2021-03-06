﻿#region License

// Copyright (c) 2014 Chandramouleswaran Ravichandran
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Runtime.Serialization;
using Hypertest.Core.Tests;
using Wide.Interfaces;

namespace Hypertest.Core.Results
{
    /// <summary>
    ///     The basic unit of a test case in the Hypertest framework
    /// </summary>
    [DataContract]
    [Serializable]
    public class TestResultModel : ContentModel
    {
        #region Members
        private TestScenario _scenario;
        #endregion

        #region CTOR
        public TestResultModel()
        {
            Initialize();
        }

        private void Initialize(bool create = true)
        {

        }
        #endregion

        #region Deserialize
        [OnDeserialized]
        private void OnDeserialized(StreamingContext context)
        {
            Initialize();
            if (_scenario != null)
            {
                _scenario.PauseStateManager();
            }
        }

        #endregion
       
        #region Properties
        [DataMember]
        public TestScenario Scenario
        {
            get { return _scenario; }
            set
            {
                _scenario = value;
                RaisePropertyChanged();
            }
        }
        #endregion

        #region Internals
        internal void SetDirty(bool p)
        {
            IsDirty = p;
        }

        internal void SetLocation(object info)
        {
            Location = info;
            IsDirty = false;
            RaisePropertyChanged("Location");
        } 
        #endregion
    }
}