using HttpContextMoq;
using Microsoft.AspNetCore.Http.Features;

namespace HttpContextMoqSessionTest;

public static class HttpContextMockExtensions
{
    public static HttpContextMock SetupInMemorySession(this HttpContextMock httpContextMock)
    {
        var inMemorySession = new InMemorySession();
        httpContextMock.Session = inMemorySession;
        httpContextMock.FeaturesMock.Mock.Setup(collection => collection.Get<ISessionFeature>()).Returns((ISessionFeature)new SessionFeatureFake()
                     {
                         Session = inMemorySession
                     });

        return httpContextMock;
    }
}