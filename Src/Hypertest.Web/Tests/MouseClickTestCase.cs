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
using System.ComponentModel;
using System.Runtime.Serialization;
using Hypertest.Core.Attributes;
using Hypertest.Web.Tests;
using Wide.Interfaces.Services;

namespace Hypertest.Core.Tests
{
    [DataContract]
    [Serializable]
    [DisplayName("Click element")]
    [Description("Clicks the first web element based on the search results")]
    [Category("Web")]
    [TestImage("Images/MouseClick.png")]
    [ScenarioTypes(typeof(WebTestScenario))]
    public class MouseClickTestCase : WebTestCase
    {
        #region CTOR
        public MouseClickTestCase()
        {
            Initialize();
        }

        private void Initialize(bool create = true)
        {
            this.Description = "Click a particular web element";
            this.MarkedForExecution = true;
        }
        #endregion

        #region Deserialize

        [OnDeserializing]
        private void OnDeserializing(StreamingContext context)
        {
            Initialize();
        }

        #endregion

        #region Override
        protected override void Body()
        {
            base.Body();
            if (this.ActualResult == TestCaseResult.Failed) return;

            //We have reached so far - this means we have an element
            try
            {
                this.Element.Click();
            }
            catch (Exception ex)
            {
                this.Log(ex.Message, LogCategory.Exception, LogPriority.High);
                this.Log(ex.StackTrace, LogCategory.Exception, LogPriority.High);
                this.ActualResult = TestCaseResult.Failed;
            }
        }
        #endregion
    }
}