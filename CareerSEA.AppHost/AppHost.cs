var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithImage("pgvector/pgvector", "pg17");
    //.WithPgAdmin(admin => admin.WithImageTag("8.11"));
// .WithDataVolume();
/*
var pythonApi = builder.AddPythonApp("aiservice", "../CareerSEA.Py", "main.py")
                       .WithHttpEndpoint(port: 8000, targetPort: 8001) 
                       .WithExternalHttpEndpoints();
*/

var pythonApi = builder.AddDockerfile("aiservice", "../CareerSEA.Py")
    .WithHttpEndpoint(port: 8001, targetPort: 8001, name: "api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("CUDA_VISIBLE_DEVICES", "-1"); // FORCE teammate's model to CPU only

var apiService = builder.AddProject<Projects.CareerSEA_ApiService>("apiservice")
    .WithHttpHealthCheck("/health")
    .WithReplicas(1)
    .WithReference(pythonApi.GetEndpoint("api"))
    .WaitFor(postgres);

var webfrontend = builder.AddProject<Projects.CareerSEA_Web>("webfrontend")
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

var ollama = builder.AddOllama("ollama")
    .WithDataVolume()
    .WithEnvironment("OLLAMA_FLASH_ATTENTION", "1")
    .WithGPUSupport();

var llama32 = ollama.AddModel("ollamaModel","llama3.2:3b");

apiService.WithReference(postgresdb)
             .WithReference(llama32);

webfrontend.WithReference(postgresdb);

builder.Build().Run();

