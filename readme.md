# DotPrompt.Sql 

This is a SQL store configured to be used with the the DotPrompt library. It currently has support for everything that you would find in DotPrompt prompt files bar a couple of things that are still in development. You can give it a prompt file and it will add the data from the prompt file to a set of related SQL tables.

In order to use this a CLI is provided which will allow you to add a prompt file to the database as long it adheres to the rules defined in DotPrompt. 

You can use the CLI with the following syntax:

```
DotPrompt.Sql.Cli ./prompts/basic.prompt ./sample.yaml 
```

The YAML file should contain the connection details to SQL Server / Azure SQL DB or Microsoft Fabric SQL DB with the following format.

```yaml
server: "myserver.database.windows.net"
database: "mydatabase"
tablename: "mytable"
username: "myuser"
password: "mypassword"
integrated_authentication: false
aad_authentication: true
```

**DotPrompt.SQL** currently supports the following features from a prompt file.

- System prompt
- User prompt
- Model
- Name
- Temperature
- MaxTokens
- Parameters
- Default

It doesn't currently support the **Few Shot Prompt** section but will shortly. It has been tested only for only username and password database access but will be tested with the others so whilst config is supported access may well break. Let me know if this is the case.

It will automatically create the tables with a unique constraint on PromptName which name which names that the names in the prompt files should be unique for now although I'll be looking at more composite uniqueness in the future with a **category table** and **categoryid** which will test for unique names within the category itself.

![Open Source Diagrams.png](..%2FOpen%20Source%20Diagrams.png)

The above is right as per v0.1 and I'll update until I get to a stable build which covers all of the features from DotPrompt.

You can review DotPrompt [here](https://github.com/elastacloud/dotprompt).


