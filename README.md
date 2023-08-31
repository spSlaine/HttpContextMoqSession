# HttpContextMoqSession

Example test project setting up mocks for Session using the HttpContextMoq (https://github.com/tiagodaraujo/HttpContextMoq)

When setting up your mock objects you can not intercept extension methods. When we are trying to setup the ISession object this includes the following more widely used methods.

``` csharp

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;

namespace Microsoft.AspNetCore.Http
{
    /// <summary>
    /// Extension methods for <see cref="ISession"/>.
    /// </summary>
    public static class SessionExtensions
    {
        /// <summary>
        /// Sets an int value in the <see cref="ISession"/>.
        /// </summary>
        /// <param name="session">The <see cref="ISession"/>.</param>
        /// <param name="key">The key to assign.</param>
        /// <param name="value">The value to assign.</param>
        public static void SetInt32(this ISession session, string key, int value)
        {
            var bytes = new byte[]
            {
                (byte)(value >> 24),
                (byte)(0xFF & (value >> 16)),
                (byte)(0xFF & (value >> 8)),
                (byte)(0xFF & value)
            };
            session.Set(key, bytes);
        }

        /// <summary>
        /// Gets an int value from <see cref="ISession"/>.
        /// </summary>
        /// <param name="session">The <see cref="ISession"/>.</param>
        /// <param name="key">The key to read.</param>
        public static int? GetInt32(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null || data.Length < 4)
            {
                return null;
            }
            return data[0] << 24 | data[1] << 16 | data[2] << 8 | data[3];
        }

        /// <summary>
        /// Sets a <see cref="string"/> value in the <see cref="ISession"/>.
        /// </summary>
        /// <param name="session">The <see cref="ISession"/>.</param>
        /// <param name="key">The key to assign.</param>
        /// <param name="value">The value to assign.</param>
        public static void SetString(this ISession session, string key, string value)
        {
            session.Set(key, Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Gets a string value from <see cref="ISession"/>.
        /// </summary>
        /// <param name="session">The <see cref="ISession"/>.</param>
        /// <param name="key">The key to read.</param>
        public static string? GetString(this ISession session, string key)
        {
            var data = session.Get(key);
            if (data == null)
            {
                return null;
            }
            return Encoding.UTF8.GetString(data);
        }

        /// <summary>
        /// Gets a byte-array value from <see cref="ISession"/>.
        /// </summary>
        /// <param name="session">The <see cref="ISession"/>.</param>
        /// <param name="key">The key to read.</param>
        public static byte[]? Get(this ISession session, string key)
        {
            session.TryGetValue(key, out var value);
            return value;
        }
    }
}
```

If you are wanting to setup the return values for various keys then you have to setup calls to the ```ISession.TryGetValue(key, out byte[]? value)``` e.g.

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

For this example I have looked at the ways that the common SetString and SetInt32 extension methods convert the values to a byte array before calling the underlying ```ISession.Set(string key, byte[] value)```.

If you are actually wanting to set and read session values as part of your tests then I would recogmend creating a simple implementation of ISession (you can store the Key value pairs in a ```IDictionary<string, byte[]?>``` object).

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