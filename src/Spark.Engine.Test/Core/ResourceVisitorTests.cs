﻿using Hl7.Fhir.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Spark.Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Spark.Engine.Test.Core
{
    [TestClass]
    public class ResourceVisitorTests
    {
//        private Regex headTailRegex = new Regex(@"(?([^\.]\[.*])(?<head>[^\.]\[(?<predicate>.*)])\.(?<tail>.*)|(?<head>[^\.])\.(?<tail>.*))");
        private Regex headTailRegex = new Regex(@"(?([^\.]*\[.*])(?<head>[^\[]*)\[(?<predicate>.*)](\.(?<tail>.*))?|(?<head>[^\.]*)(\.(?<tail>.*))?)");

        [TestMethod]
        public void TestHeadNoTail()
        {
            var test = "a";
            var match = headTailRegex.Match(test);
            Assert.AreEqual("a", match.Groups["head"].Value);
            Assert.AreEqual("", match.Groups["predicate"].Value);
            Assert.AreEqual("", match.Groups["tail"].Value);
        }

        [TestMethod]
        public void TestHeadAndTailMultipleCharacters()
        {
            var test = "ax.bx.cx";
            var match = headTailRegex.Match(test);
            Assert.AreEqual("ax", match.Groups["head"].Value);
            Assert.AreEqual("", match.Groups["predicate"].Value);
            Assert.AreEqual("bx.cx", match.Groups["tail"].Value);
        }

        [TestMethod]
        public void TestHeadWithPredicateNoTail()
        {
            var test = "a[x=y]";
            var match = headTailRegex.Match(test);
            Assert.AreEqual("a", match.Groups["head"].Value);
            Assert.AreEqual("x=y", match.Groups["predicate"].Value);
            Assert.AreEqual("", match.Groups["tail"].Value);
        }

        [TestMethod]
        public void TestHeadAndTailNoPredicate()
        {
            var test = "a.b.c";
            var match = headTailRegex.Match(test);
            Assert.AreEqual("a", match.Groups["head"].Value);
            Assert.AreEqual("", match.Groups["predicate"].Value);
            Assert.AreEqual("b.c", match.Groups["tail"].Value);
        }

        [TestMethod]
        public void TestHeadAndTailWithPredicate()
        {
            var test = "a[x.y=z].b.c";
            var match = headTailRegex.Match(test);
            Assert.AreEqual("a", match.Groups["head"].Value);
            Assert.AreEqual("x.y=z", match.Groups["predicate"].Value);
            Assert.AreEqual("b.c", match.Groups["tail"].Value);
        }

        [TestMethod]
        public void TestLongerHeadAndTailWithPredicate()
        {
            var test = "ax[yx=zx].bx";
            var match = headTailRegex.Match(test);
            Assert.AreEqual("ax", match.Groups["head"].Value);
            Assert.AreEqual("yx=zx", match.Groups["predicate"].Value);
            Assert.AreEqual("bx", match.Groups["tail"].Value);
        }

        private FhirPropertyIndex _index;
        private ResourceVisitor _sut;
        private Patient _patient;
        private int _expectedActionCounter = 0;
        private int _actualActionCounter = 0;

        [TestInitialize]
        public void TestInitialize()
        {
            _index = new FhirPropertyIndex(new List<Type> { typeof(Patient), typeof(ClinicalImpression), typeof(HumanName), typeof(CodeableConcept), typeof(Coding) });
            _sut = new ResourceVisitor(_index);
            _patient = new Patient();
            _patient.Name.Add(new HumanName().WithGiven("Sjors").AndFamily("Jansen"));
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Assert.AreEqual(_expectedActionCounter, _actualActionCounter);
        }

        [TestMethod]
        public void TestVisitNotExistingPathNoPredicate()
        {
            _sut.Visit(_patient, ob => Assert.Fail(), "not_existing_property");
        }

        [TestMethod]
        public void TestVisitSinglePathNoPredicate()
        {
            _expectedActionCounter = 1;
            _sut.Visit(_patient, ob => 
                {
                    _actualActionCounter++;
                    if (ob.GetType() != typeof(HumanName))
                        Assert.Fail();
                }, "name");
        }

        [TestMethod]
        public void TestVisitDataChoiceProperty()
        {
            _expectedActionCounter = 1;
            ClinicalImpression ci = new ClinicalImpression();
            ci.Trigger = new CodeableConcept("test.system", "test.code");
            _sut.Visit(ci, ob => 
                {
                    _actualActionCounter++;
                    if (ob.ToString() != "test.system")
                        Assert.Fail();
                }, 
                "triggerCodeableConcept.coding.system");
        }

        [TestMethod]
        public void TestVisitNestedPathNoPredicate()
        {
            _expectedActionCounter = 1;
            _sut.Visit(_patient, ob => 
                {
                    _actualActionCounter++;
                    if (ob.ToString() != "Sjors")
                            Assert.Fail();
                }, "name.given");
        }

        [TestMethod]
        public void TestVisitSinglePathWithPredicate()
        {
            _expectedActionCounter = 1;
            _patient.Name.Add(new HumanName().WithGiven("Sjimmie").AndFamily("Visser"));
            _sut.Visit(_patient, ob => 
                {
                    _actualActionCounter++;
                    if (ob.ToString() != "Sjimmie")
                        Assert.Fail();
                }, "name[given=Sjimmie].given");
        }
    }
}