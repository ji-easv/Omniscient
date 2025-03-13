workspace {
    model {
        User = person "User" "Person"

        Omniscient = softwareSystem "Search Engine System" "Allows users to search through Enron e-mails." {
            BlazorUI = container "Web Interface" "Allows the user to enter search terms." "Blazor Project" {
                IndexerClient = component "Indexer Client" "Requests data from the Indexer Microservice" ".NET Service"
                UI = component "User Interface" "Displays the requested data and allows the user to search and download e-mails" "Blazor Components"
            }

            CleanerMS = container "Cleaner Microservice" "Ingests e-mails from a local file system in the form of text files, removes the headers, and forwards the clean version to the indexer" ".NET Project" {
                CleanerService = component "Cleaner Service" "Removes the headers and formats the e-mail file"  ".NET Service"
                IFileSystemRepository = component "File System Repository" "Retrieves e-mails from local storage" ".NET Service"
                RabbitMQPublisher = component "RabbitMQ Publisher" "Sends the formatted e-mail file to a queue" ".NET Service"
            }

            IndexerMS = container "Indexer Microservice" "Handles the files and stores the files along with the result of the indexing in a database"  ".NET Project" {
                IndexerController = component "Indexer Controller" "Allows the user to filter and retrieve e-mails through HTTP" ".NET Controller"
                IndexerService = component "Indexer Service" "Finds out the frequency of word occurences and indexes files" ".NET Service"
                IndexerRepository = component "Indexer Repository" "Communicates with the database to store the indexed entries" ".NET Service"
                RabbitMQConsumer = component "RabbitMQ Consumer" "Consumes messages produced by the CleanerMS" ".NET Service"
            }

            IndexerDatabase = container "Indexer Database" "Persists indexed files, words and occurences" "PostgreSQL" {
                tags "Database"
            }

            LocalFileSystem = container "Local File System" "Provides the dataset" "File System" {
                tags "Database"
            }

            RabbitMQQueue = container "RabbitMQ Queue" "Queue for communication between microservices" "RabbitMQ" {
                tags "Queue"
            }
        }

        // Relationships
        User -> BlazorUI "Sends search terms" "HTTPS"

        BlazorUI -> IndexerMS "GET emails" "HTTPS"
        IndexerCLient -> IndexerController "GET emails" "HTTPS"

        IndexerController -> IndexerService "Uses"
        IndexerService -> IndexerRepository "Uses"
        RabbitMQConsumer -> IndexerService "Uses"
        IndexerRepository -> IndexerDatabase "Reads from" "JDBC

        CleanerService -> RabbitMQPublisher "Uses"
        CleanerService -> IFileSystemRepository "Uses"
        IFileSystemRepository -> LocalFileSystem "Reads from" "I/O Operations"

        RabbitMQPublisher -> RabbitMQQueue "Publishes to" "AMQP"
        RabbitMQConsumer -> RabbitMQQueue "Consumes from" "AMQP"

    }

    views {
        styles {
            element "Element" {
                color #ffffff
            }
            element "Person" {
                background #003459
                shape person
            }
            element "Software System" {
                background #ba1e25
            }
            element "App" {
                shape "MobileDeviceLandscape"
            }
            element "Database" {
                shape cylinder
                background #61C9A8
            }
            element "Container" {
                background #d9232b
            }
            element "Component" {
                background #E66C5A
            }
            element "WebBrowser" {
                shape WebBrowser
            }
            element "Queue" {
                shape pipe
                background #ffcc00
            }
        }
    }
}