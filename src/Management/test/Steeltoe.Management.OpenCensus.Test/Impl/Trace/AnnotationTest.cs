using Steeltoe.Management.Census.Utils;
using System;
using System.Collections.Generic;
using Xunit;
using Steeltoe.Management.Census.Stats;
namespace Steeltoe.Management.Census.Trace.Test
{
    public class AnnotationTest
    {
        [Fact]
        public void FromDescription_NullDescription()
        {
            Assert.Throws<ArgumentNullException>(() => Annotation.FromDescription(null));
        }

        [Fact]
        public void FromDescription()
        {
            IAnnotation annotation = Annotation.FromDescription("MyAnnotationText");
            Assert.Equal("MyAnnotationText", annotation.Description);
            Assert.Equal(0, annotation.Attributes.Count);
        }

        [Fact]
        public void FromDescriptionAndAttributes_NullDescription()
        {
            Assert.Throws<ArgumentNullException>(() => Annotation.FromDescriptionAndAttributes(null, new Dictionary<string, IAttributeValue>()));
        }

        [Fact]
        public void FromDescriptionAndAttributes_NullAttributes()
        {
            Assert.Throws<ArgumentNullException>(() => Annotation.FromDescriptionAndAttributes("", null));
        }

        [Fact]
        public void FromDescriptionAndAttributes()
        {
            Dictionary<string, IAttributeValue> attributes = new Dictionary<string, IAttributeValue>();
            attributes.Add(
                "MyStringAttributeKey", AttributeValue<string>.Create("MyStringAttributeValue"));
            IAnnotation annotation = Annotation.FromDescriptionAndAttributes("MyAnnotationText", attributes);
            Assert.Equal("MyAnnotationText", annotation.Description);
            Assert.Equal(attributes, annotation.Attributes);
        }

        [Fact]
        public void FromDescriptionAndAttributes_EmptyAttributes()
        {
            IAnnotation annotation =
                Annotation.FromDescriptionAndAttributes(
                    "MyAnnotationText", new Dictionary<string, IAttributeValue>());
            Assert.Equal("MyAnnotationText", annotation.Description);
            Assert.Equal(0, annotation.Attributes.Count);
        }

        [Fact]
        public void Annotation_EqualsAndHashCode()
        {
            //EqualsTester tester = new EqualsTester();
            //Map<String, AttributeValue> attributes = new HashMap<String, AttributeValue>();
            //attributes.put(
            //    "MyStringAttributeKey", AttributeValue.stringAttributeValue("MyStringAttributeValue"));
            //tester
            //    .addEqualityGroup(
            //        Annotation.fromDescription("MyAnnotationText"),
            //        Annotation.fromDescriptionAndAttributes(
            //            "MyAnnotationText", Collections.< String, AttributeValue > emptyMap()))
            //    .addEqualityGroup(
            //        Annotation.fromDescriptionAndAttributes("MyAnnotationText", attributes),
            //        Annotation.fromDescriptionAndAttributes("MyAnnotationText", attributes))
            //    .addEqualityGroup(Annotation.fromDescription("MyAnnotationText2"));
            //tester.testEquals();
        }

        [Fact]
        public void Annotation_ToString()
        {
            IAnnotation annotation = Annotation.FromDescription("MyAnnotationText");
            Assert.Contains("MyAnnotationText", annotation.ToString());
            Dictionary<string, IAttributeValue> attributes = new Dictionary<string, IAttributeValue>();
            attributes.Add(
                "MyStringAttributeKey", AttributeValue<string>.Create("MyStringAttributeValue"));
            annotation = Annotation.FromDescriptionAndAttributes("MyAnnotationText2", attributes);
            Assert.Contains("MyAnnotationText2", annotation.ToString());
            Assert.Contains(Collections.ToString(attributes), annotation.ToString());
        }
    }
}
