Equipment Borrowing System


 1. Solution Structure

-EquipmentBorrowing.Domain:
  Contains core business entities (`Student`, `Equipment`, `Borrowing`) and domain enums (`BorrowingStatus`). This layer holds pure business rules and data models. It has zero external references or dependencies on other projects or database frameworks.

-EquipmentBorrowing.Application: 
  Contains business use cases (`BorrowEquipmentService`) and repository abstractions (`IStudentRepository`, `IEquipmentRepository`, `IBorrowingRepository`). It handles workflow orchestrations, guard condition validation, and data manipulation rules while referencing only the Domain project.

-EquipmentBorrowing.Infrastructure:
  Contains technical implementation details, such as in-memory data persistence classes (`InMemoryStudentRepository`, `InMemoryEquipmentRepository`, `InMemoryBorrowingRepository`). It implements the interface contracts defined in the Application layer and references both Domain and Application.

-EquipmentBorrowing.Console:
  Serves as the entry point application to demonstrate success and failure borrowing workflows. It references Domain, Application, and Infrastructure to configure runtime dependencies.

-EquipmentBorrowing.Tests:
  Contains xUnit test classes (`UnitTest1.cs`) used to verify unit behavior across Application and Domain components without needing external services running.



 2. Dependency Direction

Executable / Future UI
          │
          ▼
     Application
       │      ▲
       ▼      │
     Domain   │
          │   │
          └───┘
     Infrastructure


-Domain depends on nothing.

-Application depends only on Domain.

-Infrastructure depends on both Application (for repository interfaces) and Domain (for entities).

-Console (Executable) depends on Domain, Application, and Infrastructure.

Use Case Mapping
-Actor: Student
-Use Case: Borrow Equipment
-Application Service: BorrowEquipmentService
-Domain Objects Used: Student, Equipment, Borrowing, BorrowingStatus
-Repository Interfaces Used: IStudentRepository, IEquipmentRepository, IBorrowingRepository
-nfrastructure Implementations Used: InMemoryStudentRepository, InMemoryEquipmentRepository, InMemoryBorrowingRepository

4. Reflection

1. Why is it important that the Domain project has no dependencies on other projects?

-Well the Domain represents the core business rules and logic of the system, so by keeping it free of external dependencies, 
business rules remain completely unaffected by technical changes like UI updates, framework upgrades, or database switches.

2. What would happen to BorrowEquipmentService if we replaced in-memory storage with SQLite?

-If you change it I think nothing inside BorrowEquipmentService would need to change. Because the service depends solely on abstractions (IStudentRepository, 
IEquipmentRepository, IBorrowingRepository), and base on my research and experience, we only need to create new SQLite repository implementations inside the Infrastructure layer 
and pass them into the service constructor.

3. Why do repository interfaces live in Application while their implementations live in Infrastructure?

-Well this one follows the Dependency Inversion Principle. The Application layer defines the contract (what data operations it needs) 
without caring how data is stored. The Infrastructure layer fulfills that contract by providing concrete technical 
implementations (in-memory lists, SQLite, PostgreSQL).

4. If a future Avalonia UI project is added, which projects will it need to reference and why?

-I think It will need to reference Application in order to execute service operations like borrowing, 
Domain in order to work with domain models like Student or Equipment, and Infrastructure 
to instantiate and inject the concrete repository implementations into the services at startup.

5. How does this 4-layer architecture make the system easier to test using unit tests?

-It allows unit tests in EquipmentBorrowing.Tests to test core business logic in isolation. 
Because BorrowEquipmentService receives repository interfaces via dependency injection, 
tests can easily supply fast mock or in-memory repositories without setting up or wiping a real database.