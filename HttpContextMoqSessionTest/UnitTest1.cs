using System.Text;
using HttpContextMoq;
using HttpContextMoq.Extensions;
using Microsoft.AspNetCore.Http;
using Shouldly;

namespace HttpContextMoqSessionTest
{
    public class UnitTest1
    {
        const string NameKey = "Name";
        const string AgeKey = "Age";

        [Fact]
        public void Test1()
        {
            // Arrange
            var context = new HttpContextMock();
            context.SetupSession();

            
            const string name = "Mike";
            var nameValue = Encoding.UTF8.GetBytes(name);


            const int age = 32;
            var ageValue = new[] {
                                      (byte)(age >> 24),
                                      (byte)(0xFF & (age >> 16)),
                                      (byte)(0xFF & (age >> 8)),
                                      (byte)(0xFF & age)
                                  };

            context.SessionMock.Mock.Setup(session => session.TryGetValue(NameKey, out nameValue)).Returns(true);
            context.SessionMock.Mock.Setup(session => session.TryGetValue(AgeKey, out ageValue)).Returns(true);

            // Act


            // Assert
            context.Session.GetString(NameKey).ShouldBe(name);
            context.Session.GetInt32(AgeKey).ShouldBe(age);
        }

        [Fact]
        public void Test2()
        {
            // Arrange
            var context = new HttpContextMock();
            context.SetupInMemorySession();
           
            const string name = "Mike";
            const int age = 32;

            // Act
            context.Session.SetString(NameKey, name);
            context.Session.SetInt32(AgeKey, age);

            // Assert
            context.Session.GetString(NameKey).ShouldBe(name);
            context.Session.GetInt32(AgeKey).ShouldBe(age);
        }
    }
}