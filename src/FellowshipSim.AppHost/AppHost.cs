// Names of the resources: "engine" is the address RemoteSimEngine uses when service discovery gives none.
const string EngineName = "engine";
const string ServiceName = "service";
const string HttpEndpoint = "http";
const string ServiceHost = "simfell.dev.localhost";
const int ServicePort = 5000;

const string EngineKeyParameter = "engine-key";
const string EngineKeyVariable = "EngineKey";
const int EngineKeyLength = 32;

var builder = DistributedApplication.CreateBuilder(args);

// The engine refuses a request without this key. FellowshipSim.Service is the only other resource with it.
var engineKey = builder.AddParameter(EngineKeyParameter, new GenerateParameterDefault { MinLength = EngineKeyLength, Special = false }, secret: true);

var engine = builder.AddProject<Projects.FellowshipSim_EngineServer>(EngineName)
    .WithEnvironment(EngineKeyVariable, engineKey);

builder.AddProject<Projects.FellowshipSim_Service>(ServiceName)
    .WithEnvironment(EngineKeyVariable, engineKey)
    .WithReference(engine)
    .WaitFor(engine)
    .WithEndpoint(HttpEndpoint, e =>
    {
        e.TargetHost = ServiceHost;
        e.Port = ServicePort;
    })
    .WithExternalHttpEndpoints();

builder.Build().Run();