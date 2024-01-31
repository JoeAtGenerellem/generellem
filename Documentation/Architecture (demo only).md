## What is Generellem?

>Note: Please visit [Architecture](https://github.com/generellem/generellem/wiki/Architecture) in the wiki for the official version of the architecture. I put this here for testing a long document during the ingestion process.

Generellem is a service that lets anyone use their own data with a Large Language Model (LLM). It comes as 2 products:

1. An open-source product. This lets developers join a community, contribute to the project, and use the software themselves. If you're interested in using the open source, check out the GitHub repository and join in.
2. A commercial SaaS product. This allows companies to quickly use the service and have a subscription for help and support. If you're interested in the commercial offering, visit the [Generellem Website](https://generellem.ai/) and sign up.

>Note: This is a new project and will evolve as the community increasingly participates and customers provide feedback.

## Characteristics

Characteristics are the guiding principles of the architecture. There are many potential characteristics. However, the ones listed here have a strong influence on how the entire project evolves.

### Adaptability

Generellem is essentially a Plug-In architecture, which by its nature requires adaptability. The core system uses Inversion of Control (IoC) to specify interchangeable dependencies to system services. The system will have built-in services for search, Retrieval-Augmented Generation (RAG), and LLMs. However, those are based on interfaces that anyone can implement with their own service. In time, Generellem will grow and people can contribute new service implementations. Adaptability is built-in from day 1.

### Maintainability

With a lot of potential contributors and a continuously growing code base, maintainability becomes ever more important. Developers need a way to approach the code and find what they need, fix bugs quickly, and easily add new features. Here are a few concepts that contribute to that:

1. Coding standards
2. Reviews
3. SOLID Principles
4. Minimizing technical dept

While no code base is perfect, making maintainability a first-class characteristic sets a North star to follow.

### Reliability

The quality has to be top-notch and the software can't crash. Although there will be times when we have issues with 3rd party service availability, network, or infrastructure issues (things that are always unpredictable), the Generellem software itself shouldn't crash. We can accomplish this by using proven techniques to manage quality:

1. Unit tests
2. Integration testing
3. Using tools like Polly for resiliency
4. Document potential exceptions

That said, Generellem is a library that other applications use. If Generellem throws an exception, it's up the consuming application to handle those exceptions.

### Other Characteristics

There are many more characteristics that could be addressed. However, many are enterprise features that are external to the core Generellem library. For example, scalability can be achieved through cloud deployments. You can have security by providing secure configuration for keys. Other characteristics, such as usability aren't applicable to a class library.

>Note: These things can change in time and we should be open to new ideas.

Now that you have an idea of the system characteristics, let's take an overview of the entire system.

## Overview

Generellem is a library that you can use in your own applications. It allows you to specify your own documents that serve as source data to an LLM. The following diagram is an overview, from a birds-eye view, on how the major components fit together to make Generellem work:

![Overview](https://github.com/generellem/generellem/assets/6180567/6d9dacf1-2ae6-441f-9d37-04a61b7c41fd)

At the bottom of the diagram is your application. It references the Generellem library. For access to local file systems or internal network resources, Generellem will have to run on a server that has access to those files and resources. If the documentation is accessible via the cloud, Generellem can run via a cloud service. 

In the middle is the NuGet package, which you would have a package reference to in your project. Whenever you reference NuGet, it will also pull in a list of dependencies. While we don't intend to have a lot of dependencies, there is a minimal number necessary for demos and basic implementation. The default implementation is based on Azure OpenAI and Azure Search. Generellem is a cross-platform library, allowing it to be used on Linux, MacOS, and Windows.

Generellem makes network calls to external providers, which include documents, search, and LLM services. These can be your internal resources, external servers, or cloud services. Generellem has connector interfaces, where anyone can build connectors that work with any external provider.

That was a high-level overview of how the big pieces fit together. Next, we drill down into Generellem itself to examine interfaces and components that make it work.

## Components

Generellem components are those parts of an application inside of the Generellem library, labeled as NuGet, and the parts that support external services, labeled as providers, shown in the last section's overview. Here, I'll dive into what those components are and explain their interactions.

The first concept from the component view is that there are two separate groups of components: those that process files and those that process user requests. 

### Processing Documents

There are two primary types of components for working with documents: Document Sources and Document Types. A Document Source is a place where the document comes from, such as a file system or web site. Document Types are documents that have a specific format or purpose, such as an Excel spreadsheet or a PDF file. The image below shows how components for Document Sources and Document Types are modeled in Generellem.

![DocumentComponents](https://github.com/generellem/generellem/assets/6180567/d34722e0-890e-4f68-9cdb-ca598ab59e1a)

In the diagram, `IDocumentSource` represents anything that is a Document Source. It's `GetDocumentsAsync` method returns a collection of `DocumentInfo` that has metadata and a `Stream` for reading the document. While the initial implementation of a document is assumed to have a textual representation, we want to leave open the possibility of other document types in the future, like audio, video, or any other binary representation of information.

Next, notice the aggregation of Document Types, represented by the `IDocumentType`. As you can see, I listed just a few of the many document types that are possible. The `GetTextAsync` method allows each `IDocumentType` implementation to read the `DocumentInfo` `Stream` and translate the contents to text. `CanProcess` is a flag that lets me put in placeholder that isn't fully supported yet and might be removed in the future (maybe feature flags?). `SupportedExtensions` indicates which file extensions we're able to process at this time. e.g. *.doc and *.docx represent different versions and formats for MS Word documents and since we have a cross-platform requirement we can't use Windows-specific libraries to process *.doc files. This can change in the future and we can update `SupportedExtensions` so if it eventually supports *.doc files. The point is that Generelem scans a Document Source and only processes the type of files and extensions that it's able to, ignoring everything else until supported.

In addition to documents, Generellem has components that help work with user questions, which we discuss next.

### Processing User Questions

At the core of handling user questions is a request and a response, respectively sending and receiving text data. Because we have a requirement to support multiple LLMs, we need to abstract the handling of the request and response, which differs between LLMs. The following figure shows how we do it:

![UserRequestResponse](https://github.com/generellem/generellem/assets/6180567/66fb289a-acbc-4e4e-b17c-a4161e7c2850)

The `IChatRequest` and `IChatResponse` represent the request and response in the simplest way possible. They just have a `Text` property of type `string`. As of this writing, Generellem supports Azure Open AI, as you can see via the `AzureOpenAIChatRequest`, implementing `IChatRequest`, and `AzureOpenAIChatResponse`, implenting `IChatResponse`. In addition to the required `Text` property, these classes have members specific to Azure Open AI: `AzureOpenAIChatRequst` has a `ChatCompletionsOptions` property for special configuration options that Azure Open AI needs and `AzureOpenAIResponse` has a `ChatCompletionsResponse` for the response options from Azure Open AI.

Now that you've seen the major components in Generellem, let's discuss their interactions and important processes.

## Processes

There are two main processes for Generellem: ingestion and querying. Ingestion is the process of scanning for documents and saving them in a format to query later. Querying is the process that searches for saved documents and builds a custom prompt for an LLM. We'll discuss these in the next two sections.

### Ingestion

Earlier, you learned about Document Sources and Documents. To support the ability to use AI with your own documents, we need to ingest the documents. This ingestion process transforms the document and saves it in a vector DB. It's part of Retrieval Augmented Generation (RAG). Here's a quick overview of the main steps of the process:

1. Search each Document Source.
2. In each Document Source, search for Documents that we're able to process.
3. Prepare each Document for storage in the vector DB.
4. Save the Document in the vector DB

The following diagram shows how Generellem does this:

![IngestionProcess](https://github.com/generellem/generellem/assets/6180567/63465687-25c8-495b-91b0-6d714f3a8c99)

Ideally, the `DocumentProcessor` runs in its own background process on a timer. That way, it can wake up periodically to see if there are any new, modified, or deleted documents to handle. For simplicity, this diagram doesn't show the loops on the Document Source and Documents. It also only shows the new document process, though Generellem does handle modified and deleted documents.

The first step is getting all of the supported Document Sources, which is the job of the `DocumentSourceFactory`. So far, we have support for `FileSystem` and `WebSite` and will be adding more. This returns all of the Document Sources, which we'll iterate through.

>Note: Behind the scenes, Generellem populates a SQLite DB with document metadata to help keep track of what documents are new, modified, or deleted. One aspect of this is that you'll want to ensure you have enough file space if you have a lot of documents. The size of each file is defined by a `DocumentHash` model that has an `int`, `string` with file hash, and another `string` with a unique identifier for the file called a `DocumentReference`.

Next, we get all of the supported documents, each represented by a `DocumentInfo` with all of the things we need to process that document. One of those things is an `IDocumentType` that's specific to each document. Here, you can see how this design creates an abstract representation of Document Sources and Document Types, allowing a single algorithm to operate on all of them the same way. The specifics are isolated to the instances of Document Source or Document Type.

So far, we're assuming that everything we're working with is text. Although that could change in the future, the current approach leaves that as a refactoring to do when and if that happens. There will certainly be fun and interesting discussions. So, the next step is to call `GetTextAsync` on the `IDocumentType`, whose specific implementation knows what type of document it's working with and can extract plain text.

Once we have the text, we'll need to transform it once again in order to save it in a vector DB. As the DB type name suggests, we need to translate the text into a vector of numbers. So, we call the `EmbedAsync` method on `RAG`, which is a property holding an `IRag` implementation that knows how to work with the mechanics of the RAG process. Another term for translating text into vectors is _embedding_, which is where the _Embed_ in `EmbedAsync` comes from. Before embedding any string, `EmbedAsync` breaks the text up into smaller pieces as part of a `TextChunk` that contains the vector representation of a piece of text. This is part of the RAG process that manages the size of documents and improves results of user queries.

Finally, we need to store the document (transformed to `TextChunk`) in the vector DB. We currently support Azure Cognitive Search nd have plans to support many more vector DBs. So, we call `IndexAsync` to save the chunked document.

Once we have documents ingested, we can start querying.

### Querying

So far, we've defined components and described the ingestion process, where documents are processed and saved into a vector DB. Everything is now in place to start querying. Rather than just send the user's query to the LLM, we're going to process it and match it with documents first to establish context. Here are the steps:

1. Clarify the user's query in the context of recent queries.
2. Search for relevant documents.
3. Query the LLM with the documents as context.

The following diagram shows how this process works:

![QueryProcess](https://github.com/generellem/generellem/assets/6180567/b2ea7646-dff0-486b-99f7-76265789d7e8)

The `AzureOpenAIOrchestrator` manages the RAG process that deals with querying the LLM. It has a `chatHistory`, which is a list of previous queries and their answers.

When the user sends their query, `AzureOpenAIOrchestrator` takes that query along with `chatHistory`, to establish context, and instructions to the LLM to clarify the user's true intent, based on the context. This results in a new query that should better represent the user's intent.

Based on the new user intent, we call `SearchAsync` on the `RAG` instance. This searches the vector DB, where ingestion stored documents, for documents matching the user intent query. This returns a list of documents that match the query.

>Note: If you recall from the ingestion process, all documents are chunked. Therefore, what we really get back are chunks of documents that match user intent.

We now have enough information to query the LLM. The documents (text chunks) set context and we call `AskAsync` on the `LLM`, using those documents to answer the question. The result is that the answer is based on your data and not some unrelated hallucination from whatever the LLM was trained on.

The last thing to do is to take the new response and update `chatHistory`, setting context for the next query.

## Deployment

Generellem will be a .NET 8 DLL and you'll be able to get it via NuGet, GitHub release page, or source code. Right now, it's only available as source code, which will change after a few more preparatory steps. Follow the guidance in [Adding Generellem to Your App](https://github.com/generellem/generellem/wiki/Adding-Generellem-to-Your-App) for ideas on how to incorporate it into your app.

## Governance 

I haven't decided what will go in this section exactly. I'm thinking along the lines of change notes for things that change the architecture significantly. Also, we'll need to specify why we did things certain ways so we can go back and understand what the evolution of the project is. For day-to-day operations, we have a [Generellem Project](https://github.com/users/generellem/projects/2) to track tasks.
