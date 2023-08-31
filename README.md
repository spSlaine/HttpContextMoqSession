# HttpContextMoqSession

Example test project setting up mocks for Session using the HttpContextMoq (https://github.com/tiagodaraujo/HttpContextMoq)

When setting up your mock objects you can not intercept extension methods. So when we try and setup of mock of the ISession object we can not use the following more widely used extension methods.

* SetInt32(string key, int value)
* GetInt32(string key)
* SetString(string key, string value)
* GetString(string key)
* Get(string key)

If you do you get the following error message

> System.NotSupportedException : Unsupported expression: x => x.Get(string key)
> 
> Extension methods (here: SessionExtensions.Get) may not be used in setup / verification expressions.

If you are wanting to setup the return values for various keys then you have to setup calls to the ```ISession.TryGetValue(key, out byte[]? value)``` method.

``` csharp
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
    }
}
```

For this example looking at the ways that the common ```SetString()``` and ```SetInt32()``` extension methods convert the values to a byte array before calling the underlying ```ISession.Set(string key, byte[] value)``` method.

If you are actually wanting to set and read session values as part of your tests then I would recommend creating a simple implementation of ISession (you can store the Key value pairs in a ```IDictionary<string, byte[]?>``` object).

e.g.

``` csharp
public class InMemorySession : ISession
{

    private readonly IDictionary<string, byte[]?> _dictionary = new Dictionary<string, byte[]?>();

    public InMemorySession()
    {

    }

    /// <inheritdoc />
    public Task LoadAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CommitAsync(CancellationToken cancellationToken = new CancellationToken())
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public bool TryGetValue(string key, out byte[]? value)
    {
        return _dictionary.TryGetValue(key, out value);
    }

    /// <inheritdoc />
    public void Set(string key, byte[] value)
    {
        _dictionary.Add(key, value);
    }

    /// <inheritdoc />
    public void Remove(string key)
    {
        _dictionary.Remove(key);
    }

    /// <inheritdoc />
    public void Clear()
    {
        _dictionary.Clear();
    }

    /// <inheritdoc />
    public bool IsAvailable => true;

    /// <inheritdoc />
    public string Id => nameof(InMemorySession);

    /// <inheritdoc />
    public IEnumerable<string> Keys => _dictionary.Keys;
}
```

Setup HttpContentMoq to use this, rather than their mocked object

``` csharp
var context = new HttpContextMock();
var inMemorySession = new InMemorySession();
context.Session = inMemorySession;
context.FeaturesMock.Mock.Setup(collection => collection.Get<ISessionFeature>()).Returns((ISessionFeature)new SessionFeatureFake()
                {
                    Session = inMemorySession
                });

```

You can then just use the HttpContextMock.Session object as normal

```csharp
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

```
