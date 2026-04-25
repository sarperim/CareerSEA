using System.IO;

var builder = DistributedApplication.CreateBuilder(args);

var pythonModelPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../CareerSEA.Py/model"));
var escoTitlesPath = Path.GetFullPath(Path.Combine(builder.AppHostDirectory, "../CareerSEA.Web/CareerSEA.Web/JobTitles.cs"));

var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg17");

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithEnvironment("OLLAMA_FLASH_ATTENTION", "1");

var qwen = ollama.AddModel("ollamaModel", "qwen2.5:3b");

var pythonApi = builder.AddDockerfile("aiservice", "../CareerSEA.Py")
    .WithHttpEndpoint(port: 8001, targetPort: 8001, name: "api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("CUDA_VISIBLE_DEVICES", "-1") // FORCE teammate's model to CPU only
    .WithEnvironment("MODEL_DIR", "/app/model")
    .WithEnvironment("ESCO_TITLES_PATH", "/app/data/JobTitles.cs")
    .WithEnvironment("OLLAMA_HOST", ollama.GetEndpoint("http"))
    .WithBindMount(pythonModelPath, "/app/model", isReadOnly: true)
    .WithBindMount(escoTitlesPath, "/app/data/JobTitles.cs", isReadOnly: true)
    .WaitFor(qwen);

var apiService = builder.AddProject<Projects.CareerSEA_ApiService>("apiservice", launchProfileName: "http")
    .WithHttpHealthCheck("/health")
    .WithReplicas(1)
    .WithReference(pythonApi.GetEndpoint("api"))
    .WaitFor(postgres)
    .WaitFor(pythonApi);

var webfrontend = builder.AddProject<Projects.CareerSEA_Web>("webfrontend", launchProfileName: "http")
    .WithExternalHttpEndpoints()
    .WithHttpHealthCheck("/health")
    .WithReference(apiService)
    .WaitFor(apiService);

//var dbPassword = builder.AddParameter("postgres-password", secret: true);

var postgresdb = postgres.AddDatabase("webAppDb");
var vectordb = postgres.AddDatabase("vectorDb");
/*
builder.AddContainer("pgadmin", "dpage/pgadmin4")
    .WithHttpEndpoint(targetPort: 80, name: "pgadmin-http")
    .WithEnvironment("PGADMIN_DEFAULT_EMAIL", "sarp.ercan@metu.edu.tr")
    .WithEnvironment("PGADMIN_DEFAULT_PASSWORD", "0000")
    .WithVolume("pgadmin-data", "/var/lib/pgadmin")
    .WithReference(postgres);
*/

apiService.WithReference(postgresdb);

webfrontend.WithReference(postgresdb);

builder.Build().Run();
