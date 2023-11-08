﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Azure.Messaging.EventHubs;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Sdk.Generators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Microsoft.Azure.Functions.SdkGeneratorTests
{
    public class FunctionExecutorGeneratorTests
    {
        // A super set of assemblies we need for all tests in the file.
        private readonly Assembly[] _referencedAssemblies = new[]
        {
            typeof(HttpTriggerAttribute).Assembly,
            typeof(FunctionAttribute).Assembly,
            typeof(QueueTriggerAttribute).Assembly,
            typeof(EventHubTriggerAttribute).Assembly,
            typeof(QueueMessage).Assembly,
            typeof(EventData).Assembly,
            typeof(BlobInputAttribute).Assembly,
            typeof(LoggingServiceCollectionExtensions).Assembly,
            typeof(ServiceProviderServiceExtensions).Assembly,
            typeof(ServiceCollection).Assembly,
            typeof(ILogger).Assembly,
            typeof(IConfiguration).Assembly,
            typeof(HostBuilder).Assembly,
            typeof(IHostBuilder).Assembly
        };

        [Fact]
        public async Task FunctionsFromMultipleClasses()
        {
            const string inputSourceCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
namespace MyCompany
{
    public class MyHttpTriggers
    {
        [Function(""FunctionA"")]
        public HttpResponseData Foo([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r, FunctionContext c)
        {
            return r.CreateResponse(System.Net.HttpStatusCode.OK);
        }
        
        private int Foo(int x) => x * x;
    }
    public class MyHttpTriggers2
    {
        [Function(""FunctionB"")]
        public HttpResponseData Bar([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r)
        {
            return r.CreateResponse(System.Net.HttpStatusCode.OK);
        }
        
        private int Foo(int x) => x * x;
    }
    public static class Foo
    {
        [Function(""ProcessOrder2"")]
        public static async Task<string> MyAsyncStaticMethod([QueueTrigger(""foo"")] string q) => q;
    }

    public class QueueTriggers
    {
        private readonly ILogger<QueueTriggers> _logger;

        public QueueTriggers(ILogger<QueueTriggers> logger)
        {
            _logger = logger;
        }

        [Function(nameof(QueueTriggers))]
        public void Run([QueueTrigger(""myqueue-items"")] QueueMessage message)
        {
            _logger.LogInformation($""Queue message: {message.MessageText}"");
        }

        [Function(""Run2"")]
        public void Run2([QueueTrigger(""myqueue-items"")] string message)
        {
            _logger.LogInformation($""Queue message: {message}"");
        }
    }
}
";
            var expectedOutput = $@"// <auto-generated/>
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Invocation;
namespace TestProject
{{
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal class DirectFunctionExecutor : IFunctionExecutor
    {{
        private readonly IFunctionActivator _functionActivator;
        private readonly Dictionary<string, Type> types = new()
        {{
            {{ ""MyCompany.MyHttpTriggers"", Type.GetType(""MyCompany.MyHttpTriggers"")! }},
            {{ ""MyCompany.MyHttpTriggers2"", Type.GetType(""MyCompany.MyHttpTriggers2"")! }},
            {{ ""MyCompany.QueueTriggers"", Type.GetType(""MyCompany.QueueTriggers"")! }}
        }};

        public DirectFunctionExecutor(IFunctionActivator functionActivator)
        {{
            _functionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
        }}

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(FunctionContext context)
        {{
            var inputBindingFeature = context.Features.Get<IFunctionInputBindingFeature>()!;
            var inputBindingResult = await inputBindingFeature.BindFunctionInputAsync(context)!;
            var inputArguments = inputBindingResult.Values;

            if (string.Equals(context.FunctionDefinition.EntryPoint, ""MyCompany.MyHttpTriggers.Foo"", StringComparison.Ordinal))
            {{
                var instanceType = types[""MyCompany.MyHttpTriggers""];
                var i = _functionActivator.CreateInstance(instanceType, context) as global::MyCompany.MyHttpTriggers;
                context.GetInvocationResult().Value = i.Foo((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0], (global::Microsoft.Azure.Functions.Worker.FunctionContext)inputArguments[1]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""MyCompany.MyHttpTriggers2.Bar"", StringComparison.Ordinal))
            {{
                var instanceType = types[""MyCompany.MyHttpTriggers2""];
                var i = _functionActivator.CreateInstance(instanceType, context) as global::MyCompany.MyHttpTriggers2;
                context.GetInvocationResult().Value = i.Bar((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""MyCompany.Foo.MyAsyncStaticMethod"", StringComparison.Ordinal))
            {{
                context.GetInvocationResult().Value = await global::MyCompany.Foo.MyAsyncStaticMethod((string)inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""MyCompany.QueueTriggers.Run"", StringComparison.Ordinal))
            {{
                var instanceType = types[""MyCompany.QueueTriggers""];
                var i = _functionActivator.CreateInstance(instanceType, context) as global::MyCompany.QueueTriggers;
                i.Run((global::Azure.Storage.Queues.Models.QueueMessage)inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""MyCompany.QueueTriggers.Run2"", StringComparison.Ordinal))
            {{
                var instanceType = types[""MyCompany.QueueTriggers""];
                var i = _functionActivator.CreateInstance(instanceType, context) as global::MyCompany.QueueTriggers;
                i.Run2((string)inputArguments[0]);
            }}
        }}
    }}
{GetExpectedExtensionMethodCode()}
}}".Replace("'", "\"");

            await TestHelpers.RunTestAsync<Worker.Sdk.Generators.FunctionExecutorGenerator>(
                _referencedAssemblies,
                inputSourceCode,
                Constants.FileNames.GeneratedFunctionExecutor,
                expectedOutput);
        }

        [Fact]
        public async Task MultipleFunctionsDependencyInjection()
        {
            string inputSourceCode = @"
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyCompany
{
    public class MyHttpTriggers
    {
        private readonly ILogger _logger;
        public MyHttpTriggers(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<MyHttpTriggers>();
        }

        [Function(""Function1"")]
        public HttpResponseData Run1([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r)
            => r.CreateResponse(System.Net.HttpStatusCode.OK);

        [Function(""Function2"")]
        public HttpResponseData Run2([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r, FunctionContext c)
        {
            return r.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        private int Foo(int x) => x * x;
    }
}
";

            var expectedOutput = @$"// <auto-generated/>
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Invocation;
namespace MyCompany.MyProject.MyApp
{{
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal class DirectFunctionExecutor : IFunctionExecutor
    {{
        private readonly IFunctionActivator _functionActivator;
        private readonly Dictionary<string, Type> types = new()
        {{
            {{ ""MyCompany.MyHttpTriggers"", Type.GetType(""MyCompany.MyHttpTriggers"")! }}
        }};

        public DirectFunctionExecutor(IFunctionActivator functionActivator)
        {{
            _functionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
        }}

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(FunctionContext context)
        {{
            var inputBindingFeature = context.Features.Get<IFunctionInputBindingFeature>()!;
            var inputBindingResult = await inputBindingFeature.BindFunctionInputAsync(context)!;
            var inputArguments = inputBindingResult.Values;

            if (string.Equals(context.FunctionDefinition.EntryPoint, ""MyCompany.MyHttpTriggers.Run1"", StringComparison.Ordinal))
            {{
                var instanceType = types[""MyCompany.MyHttpTriggers""];
                var i = _functionActivator.CreateInstance(instanceType, context) as global::MyCompany.MyHttpTriggers;
                context.GetInvocationResult().Value = i.Run1((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""MyCompany.MyHttpTriggers.Run2"", StringComparison.Ordinal))
            {{
                var instanceType = types[""MyCompany.MyHttpTriggers""];
                var i = _functionActivator.CreateInstance(instanceType, context) as global::MyCompany.MyHttpTriggers;
                context.GetInvocationResult().Value = i.Run2((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0], (global::Microsoft.Azure.Functions.Worker.FunctionContext)inputArguments[1]);
            }}
        }}
    }}
{GetExpectedExtensionMethodCode()}
}}".Replace("'", "\"");

            // override the namespace value for generated types using msbuild property.
            var buildPropertiesDict = new Dictionary<string, string>()
            {
                {  Constants.BuildProperties.GeneratedCodeNamespace, "MyCompany.MyProject.MyApp"}
            };

            await TestHelpers.RunTestAsync<FunctionExecutorGenerator>(
                _referencedAssemblies,
                inputSourceCode,
                Constants.FileNames.GeneratedFunctionExecutor,
                expectedOutput,
                buildPropertiesDictionary: buildPropertiesDict);
        }

        [Fact]
        public async Task StaticMethods()
        {
            var inputSourceCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Azure.Messaging.EventHubs;
using System.IO;

namespace FunctionApp26
{
    public static class MyQTriggers
    {
        [Function(""ProcessOrder1"")]
        public static Task MyTaskStaticMethod([QueueTrigger(""foo"")] string q)
        {
            return Task.CompletedTask;
        }
        [Function(""ProcessOrder2"")]
        public static async Task<string> MyAsyncStaticMethod([QueueTrigger(""foo"")] string q) => q;

        [Function(""ProcessOrder3"")]
        public static void MyVoidStaticMethod([QueueTrigger(""foo"")] string q)
        {
        }
        [Function(""ProcessOrder4"")]
        public static async Task<int> MyAsyncStaticMethodWithReturn(
                    [QueueTrigger(""foo"")] string q,
                    [BlobInput(""test-samples/sample1.txt"")] string myBlob)
        {
            return q.Length + myBlob.Length;
        }
        [Function(""ProcessOrder5"")]
        public static async ValueTask<string> MyValueTaskOfTStaticAsyncMethod([QueueTrigger(""foo"")] string q)
        {
            return q;
        }
        [Function(""ProcessOrder6"")]
        public static ValueTask MyValueTaskStaticAsyncMethod2([QueueTrigger(""foo"")] string q)
        {
            return ValueTask.CompletedTask;
        }
    }
    public class BlobTriggers
    {
        [Function(nameof(BlobTriggers))]
        public static async Task Run([BlobTrigger(""items/{name}"", Connection = ""ConStr"")] Stream stream, string name)
        {
            using var blobStreamReader = new StreamReader(stream);
            var content = await blobStreamReader.ReadToEndAsync();
        }
    }
    public class EventHubTriggers
    {
        [Function(""Run1"")]
        public static void Run1([EventHubTrigger(""items"", Connection = ""EventHubConnection"")] EventData[] data)
        {
        }
        [Function(nameof(Run2))]
        [EventHubOutput(""dest"", Connection = ""EHConnection"")]
        public static string Run2([EventHubTrigger(""queue"", Connection = ""EventHubConnection"", IsBatched = false)] EventData eventData)
        {
            return eventData.MessageId;
        }
        [Function(""RunAsync1"")]
        public static Task RunAsync1([EventHubTrigger(""items"", Connection = ""Con"")] EventData[] data)
        {
            return Task.CompletedTask;
        }
        [Function(""RunAsync2"")]
        public static async Task RunAsync2([EventHubTrigger(""items"", Connection = ""Con"")] EventData[] data) => await Task.Delay(10);
    }
}".Replace("'", "\"");
            var expectedOutput = @$"// <auto-generated/>
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Invocation;
namespace TestProject
{{
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal class DirectFunctionExecutor : IFunctionExecutor
    {{
        private readonly IFunctionActivator _functionActivator;
        
        public DirectFunctionExecutor(IFunctionActivator functionActivator)
        {{
            _functionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
        }}

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(FunctionContext context)
        {{
            var inputBindingFeature = context.Features.Get<IFunctionInputBindingFeature>()!;
            var inputBindingResult = await inputBindingFeature.BindFunctionInputAsync(context)!;
            var inputArguments = inputBindingResult.Values;

            if (string.Equals(context.FunctionDefinition.EntryPoint, ""FunctionApp26.MyQTriggers.MyTaskStaticMethod"", StringComparison.Ordinal))
            {{
                await global::FunctionApp26.MyQTriggers.MyTaskStaticMethod((string)inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""FunctionApp26.MyQTriggers.MyAsyncStaticMethod"", StringComparison.Ordinal))
            {{
                context.GetInvocationResult().Value = await global::FunctionApp26.MyQTriggers.MyAsyncStaticMethod((string)inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""FunctionApp26.MyQTriggers.MyVoidStaticMethod"", StringComparison.Ordinal))
            {{
                global::FunctionApp26.MyQTriggers.MyVoidStaticMethod((string)inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""FunctionApp26.MyQTriggers.MyAsyncStaticMethodWithReturn"", StringComparison.Ordinal))
            {{
                context.GetInvocationResult().Value = await global::FunctionApp26.MyQTriggers.MyAsyncStaticMethodWithReturn((string)inputArguments[0], (string)inputArguments[1]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""FunctionApp26.MyQTriggers.MyValueTaskOfTStaticAsyncMethod"", StringComparison.Ordinal))
            {{
                context.GetInvocationResult().Value = await global::FunctionApp26.MyQTriggers.MyValueTaskOfTStaticAsyncMethod((string)inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""FunctionApp26.MyQTriggers.MyValueTaskStaticAsyncMethod2"", StringComparison.Ordinal))
            {{
                await global::FunctionApp26.MyQTriggers.MyValueTaskStaticAsyncMethod2((string)inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""FunctionApp26.BlobTriggers.Run"", StringComparison.Ordinal))
            {{
                await global::FunctionApp26.BlobTriggers.Run((global::System.IO.Stream)inputArguments[0], (string)inputArguments[1]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""FunctionApp26.EventHubTriggers.Run1"", StringComparison.Ordinal))
            {{
                global::FunctionApp26.EventHubTriggers.Run1((global::Azure.Messaging.EventHubs.EventData[])inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""FunctionApp26.EventHubTriggers.Run2"", StringComparison.Ordinal))
            {{
                context.GetInvocationResult().Value = global::FunctionApp26.EventHubTriggers.Run2((global::Azure.Messaging.EventHubs.EventData)inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""FunctionApp26.EventHubTriggers.RunAsync1"", StringComparison.Ordinal))
            {{
                await global::FunctionApp26.EventHubTriggers.RunAsync1((global::Azure.Messaging.EventHubs.EventData[])inputArguments[0]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""FunctionApp26.EventHubTriggers.RunAsync2"", StringComparison.Ordinal))
            {{
                await global::FunctionApp26.EventHubTriggers.RunAsync2((global::Azure.Messaging.EventHubs.EventData[])inputArguments[0]);
            }}
        }}
    }}
{GetExpectedExtensionMethodCode()}
}}".Replace("'", "\"");

            await TestHelpers.RunTestAsync<Worker.Sdk.Generators.FunctionExecutorGenerator>(
                _referencedAssemblies,
                inputSourceCode,
                Constants.FileNames.GeneratedFunctionExecutor,
                expectedOutput);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task VerifyAutoConfigureStartupTypeEmitted(bool includeAutoStartupType)
        {
            string inputSourceCode = @"
using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MyCompany
{
    public class MyHttpTriggers
    {
        [Function(""Function1"")]
        public HttpResponseData Run1([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r)
        {
            return r.CreateResponse(System.Net.HttpStatusCode.OK);
        }
    }
}
";

            var expectedOutput = @$"// <auto-generated/>
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Invocation;
namespace TestProject
{{
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal class DirectFunctionExecutor : IFunctionExecutor
    {{
        private readonly IFunctionActivator _functionActivator;
        private readonly Dictionary<string, Type> types = new()
        {{
            {{ ""MyCompany.MyHttpTriggers"", Type.GetType(""MyCompany.MyHttpTriggers"")! }}
        }};

        public DirectFunctionExecutor(IFunctionActivator functionActivator)
        {{
            _functionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
        }}

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(FunctionContext context)
        {{
            var inputBindingFeature = context.Features.Get<IFunctionInputBindingFeature>()!;
            var inputBindingResult = await inputBindingFeature.BindFunctionInputAsync(context)!;
            var inputArguments = inputBindingResult.Values;

            if (string.Equals(context.FunctionDefinition.EntryPoint, ""MyCompany.MyHttpTriggers.Run1"", StringComparison.Ordinal))
            {{
                var instanceType = types[""MyCompany.MyHttpTriggers""];
                var i = _functionActivator.CreateInstance(instanceType, context) as global::MyCompany.MyHttpTriggers;
                context.GetInvocationResult().Value = i.Run1((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0]);
            }}
        }}
    }}
{GetExpectedExtensionMethodCode(includeAutoStartupType: includeAutoStartupType)}
}}".Replace("'", "\"");

            var buildPropertiesDict = new Dictionary<string, string>()
            {
                {  Constants.BuildProperties.AutoRegisterGeneratedFunctionsExecutor, includeAutoStartupType.ToString()}
            };

            await TestHelpers.RunTestAsync<FunctionExecutorGenerator>(
                _referencedAssemblies,
                inputSourceCode,
                Constants.FileNames.GeneratedFunctionExecutor,
                expectedOutput,
                buildPropertiesDictionary: buildPropertiesDict);
        }

        [Fact]
        public async Task ClassWithSameNameAsNamespace()
        {
            const string inputSourceCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
namespace TestProject
{
    public class TestProject
    {
        [Function(""FunctionA"")]
        public HttpResponseData Foo([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r, FunctionContext c)
        {
            return r.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        [Function(""FunctionB"")]
        public static HttpResponseData FooStatic([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r, FunctionContext c)
        {
            return r.CreateResponse(System.Net.HttpStatusCode.OK);
        }
    }
}
";
            var expectedOutput = $@"// <auto-generated/>
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Invocation;
namespace TestProject
{{
    [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
    internal class DirectFunctionExecutor : IFunctionExecutor
    {{
        private readonly IFunctionActivator _functionActivator;
        private readonly Dictionary<string, Type> types = new()
        {{
            {{ ""TestProject.TestProject"", Type.GetType(""TestProject.TestProject"")! }}
        }};

        public DirectFunctionExecutor(IFunctionActivator functionActivator)
        {{
            _functionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
        }}

        /// <inheritdoc/>
        public async ValueTask ExecuteAsync(FunctionContext context)
        {{
            var inputBindingFeature = context.Features.Get<IFunctionInputBindingFeature>()!;
            var inputBindingResult = await inputBindingFeature.BindFunctionInputAsync(context)!;
            var inputArguments = inputBindingResult.Values;

            if (string.Equals(context.FunctionDefinition.EntryPoint, ""TestProject.TestProject.Foo"", StringComparison.Ordinal))
            {{
                var instanceType = types[""TestProject.TestProject""];
                var i = _functionActivator.CreateInstance(instanceType, context) as global::TestProject.TestProject;
                context.GetInvocationResult().Value = i.Foo((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0], (global::Microsoft.Azure.Functions.Worker.FunctionContext)inputArguments[1]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""TestProject.TestProject.FooStatic"", StringComparison.Ordinal))
            {{
                context.GetInvocationResult().Value = global::TestProject.TestProject.FooStatic((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0], (global::Microsoft.Azure.Functions.Worker.FunctionContext)inputArguments[1]);
            }}
        }}
    }}
{GetExpectedExtensionMethodCode()}
}}".Replace("'", "\"");

            await TestHelpers.RunTestAsync<Worker.Sdk.Generators.FunctionExecutorGenerator>(
                _referencedAssemblies,
                inputSourceCode,
                Constants.FileNames.GeneratedFunctionExecutor,
                expectedOutput);
        }

        [Fact]
        public async Task FunctionsWithSameNameExceptForCasing()
        {
            const string inputSourceCode = @"
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
namespace MyCompany
{
    public class MyHttpTriggers
    {
        [Function(""FunctionA"")]
        public HttpResponseData Hello([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r, FunctionContext c)
        {
            return r.CreateResponse(System.Net.HttpStatusCode.OK);
        }

        [Function(""FunctionB"")]
        public static HttpResponseData HELLO([HttpTrigger(AuthorizationLevel.User, ""get"")] HttpRequestData r, FunctionContext c)
        {
            return r.CreateResponse(System.Net.HttpStatusCode.OK);
        }
    }
}
";
            var expectedOutput = $@"// <auto-generated/>
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Invocation;
namespace TestProject
{{
    internal class DirectFunctionExecutor : IFunctionExecutor
    {{
        private readonly IFunctionActivator _functionActivator;
        private readonly Dictionary<string, Type> types = new()
        {{
            {{ ""MyCompany.MyHttpTriggers"", Type.GetType(""MyCompany.MyHttpTriggers"")! }}
        }};

        public DirectFunctionExecutor(IFunctionActivator functionActivator)
        {{
            _functionActivator = functionActivator ?? throw new ArgumentNullException(nameof(functionActivator));
        }}

        public async ValueTask ExecuteAsync(FunctionContext context)
        {{
            var inputBindingFeature = context.Features.Get<IFunctionInputBindingFeature>()!;
            var inputBindingResult = await inputBindingFeature.BindFunctionInputAsync(context)!;
            var inputArguments = inputBindingResult.Values;

            if (string.Equals(context.FunctionDefinition.EntryPoint, ""MyCompany.MyHttpTriggers.Hello"", StringComparison.Ordinal))
            {{
                var instanceType = types[""MyCompany.MyHttpTriggers""];
                var i = _functionActivator.CreateInstance(instanceType, context) as global::MyCompany.MyHttpTriggers;
                context.GetInvocationResult().Value = i.Hello((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0], (global::Microsoft.Azure.Functions.Worker.FunctionContext)inputArguments[1]);
            }}
            else if (string.Equals(context.FunctionDefinition.EntryPoint, ""MyCompany.MyHttpTriggers.HELLO"", StringComparison.Ordinal))
            {{
                context.GetInvocationResult().Value = global::MyCompany.MyHttpTriggers.HELLO((global::Microsoft.Azure.Functions.Worker.Http.HttpRequestData)inputArguments[0], (global::Microsoft.Azure.Functions.Worker.FunctionContext)inputArguments[1]);
            }}
        }}
    }}
{GetExpectedExtensionMethodCode()}
}}".Replace("'", "\"");

            await TestHelpers.RunTestAsync<Worker.Sdk.Generators.FunctionExecutorGenerator>(
                _referencedAssemblies,
                inputSourceCode,
                Constants.FileNames.GeneratedFunctionExecutor,
                expectedOutput);
        }

        private static string GetExpectedExtensionMethodCode(bool includeAutoStartupType = false)
        {
            if (includeAutoStartupType)
            {
                return """

                            /// <summary>
                            /// Extension methods to enable registration of the custom <see cref="IFunctionExecutor"/> implementation generated for the current worker.
                            /// </summary>
                            public static class FunctionExecutorHostBuilderExtensions
                            {
                                ///<summary>
                                /// Configures an optimized function executor to the invocation pipeline.
                                ///</summary>
                                public static IHostBuilder ConfigureGeneratedFunctionExecutor(this IHostBuilder builder)
                                {
                                    return builder.ConfigureServices(s => 
                                    {
                                        s.AddSingleton<IFunctionExecutor, DirectFunctionExecutor>();
                                    });
                                }
                            }
                            /// <summary>
                            /// Auto startup class to register the custom <see cref="IFunctionExecutor"/> implementation generated for the current worker.
                            /// </summary>
                            [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Never)]
                            public class FunctionExecutorAutoStartup : IAutoConfigureStartup
                            {
                                /// <summary>
                                /// Configures the <see cref="IHostBuilder"/> to use the custom <see cref="IFunctionExecutor"/> implementation generated for the current worker.
                                /// </summary>
                                /// <param name="hostBuilder">The <see cref="IHostBuilder"/> instance to use for service registration.</param>
                                public void Configure(IHostBuilder hostBuilder)
                                {
                                    hostBuilder.ConfigureGeneratedFunctionExecutor();
                                }
                            }
                        """;
            }

            return """

                        /// <summary>
                        /// Extension methods to enable registration of the custom <see cref="IFunctionExecutor"/> implementation generated for the current worker.
                        /// </summary>
                        public static class FunctionExecutorHostBuilderExtensions
                        {
                            ///<summary>
                            /// Configures an optimized function executor to the invocation pipeline.
                            ///</summary>
                            public static IHostBuilder ConfigureGeneratedFunctionExecutor(this IHostBuilder builder)
                            {
                                return builder.ConfigureServices(s => 
                                {
                                    s.AddSingleton<IFunctionExecutor, DirectFunctionExecutor>();
                                });
                            }
                        }
                    """;
        }
    }
}
