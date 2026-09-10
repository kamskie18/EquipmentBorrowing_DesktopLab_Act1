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





 Laboratory Activity 2 — Avalonia UI and MVVM

1. Desktop Project

EquipmentBorrowing.Desktop is the presentation layer added in Laboratory Activity 2. It contains
Avalonia Views (XAML) under Views/ and ViewModels under ViewModels/, along with the application's
composition root (App.axaml.cs), which wires dependency injection for the whole application.

The Desktop project references Application, Infrastructure, and Domain so it can display data,
collect user input, and invoke existing application services. However, Domain and Application have
no reference to Avalonia or to the Desktop project at all — they remain completely independent of
the UI framework, exactly as they were in Laboratory Activity 1. This means the borrowing rules
implemented in Activity 1 did not need to change or be duplicated to support the new interface.

 2. Updated Architecture

Avalonia View
     │  Binding / Command
     ▼
ViewModel
     │  Application Operation
     ▼
Application Service
     │
     ├──────────► Domain
     ▼
Repository Interface
     ▲
     │
Infrastructure Implementation

- View (EquipmentView.axaml, BorrowingsView.axaml) binds to ViewModel properties and commands.
- ViewModel (EquipmentViewModel, BorrowingsViewModel, MainWindowViewModel) holds presentation
  state and calls Application services — it contains no business rules of its own.
- Application Service (BorrowEquipmentService, ReturnEquipmentService) coordinates Domain objects
  and Repository interfaces to perform the actual use case.
- Repository Interface / Infrastructure Implementation (unchanged from Activity 1) still provide
  data access through in-memory repositories, registered as Singletons in the Desktop project's
  composition root so all ViewModels share the same underlying data.

3. Borrow Equipment Flow

The user selects a student and a piece of equipment on the Equipment view, then presses
"Borrow Equipment." This triggers EquipmentViewModel's BorrowCommand. The ViewModel first performs
presentation validation — checking that both a student and equipment were actually selected — and
displays a message if not. If both are selected, it calls BorrowEquipmentService.BorrowAsync(),
passing only the selected student and equipment IDs.

The service performs the real business validation: confirming the student exists and is allowed to
borrow, the equipment exists and is available, and the student has not reached the maximum active
borrowings, all using the repository interfaces from Activity 1. If every rule passes, it creates a
new Borrowing, marks the equipment unavailable, and persists both changes. The result (success or a
specific failure reason) is returned to the ViewModel, which displays it as a status message and
reloads the equipment list so the updated availability is immediately visible.

 4. Return Equipment Flow

The user switches to the Active Borrowings view (which reloads its list from the repository each
time it is opened, ensuring it reflects the current state), selects an active borrowing, and presses
"Return Equipment." This triggers BorrowingsViewModel's ReturnCommand, which first checks that a
borrowing was actually selected. It then calls ReturnEquipmentService.ReturnAsync() with the
selected borrowing's ID.

The service locates the borrowing, verifies it has not already been returned, marks it as Returned,
marks the associated equipment as available again, and persists both updates through the repository
interfaces. The result is returned to the ViewModel, which displays a success or failure message and
reloads the active borrowings list. Navigating back to the Equipment view reflects the newly
available equipment as well, since that list is also reloaded on navigation.

 5. Architectural Reflection

1. Why should the View not call a repository directly?
   
   -Well if the View called the repository directly, it would basically be doing the job that's supposed to belong to the Application layer. 
   The View would need to know how data is stored (like it's a List right now, but what if we switch to SQLite later?), and worse, 
   it would skip all the validation rules like checking if the student is allowed to borrow or if the equipment is available. 
   That logic has to run somewhere, and it shouldn't be inside a button click. Keeping the View away from the repository 
   means the View only worries about what to show and what to bind, not how the data actually works.

2. Why should business rules not be implemented in the ViewModel?
   
   -Because then we'd basically have the same rule written in two different places, once in BorrowEquipmentService 
   from Activity 1, and again in the ViewModel if we tried to check things like equipment.IsAvailable ourselves before 
   calling the service. If we ever needed to change a rule (like increasing the max borrow limit from 3 to 5), 
   we'd have to remember to update it in both places, and it's really easy to forget one and end up with bugs. 
   Also, the ViewModel is supposed to be about the UI side of things, not about deciding whether a transaction is actually allowed to happen.

3. What is the responsibility of the ViewModel?
  
  -Basically the ViewModel is the "middleman" between the View and the actual application logic. 
   It holds the stuff the screen needs to show (like the equipment list, the selected student, status messages), 
   and it has the commands that get triggered when the user clicks something. 
   But when it comes to actually deciding if a borrow/return is allowed, it just passes that off to the Application service and waits for the result. 
   So its job is more like "manage what's on screen and talk to the services," not "decide if the operation is valid."

4. Why can the existing Application layer work without knowing that Avalonia is being used?
   
   -Because BorrowEquipmentService and the repository interfaces don't reference Avalonia anywhere, 
   they only depend on the Domain layer and their own interfaces. They don't care who's calling them, 
   whether it's a console app like in Activity 1, this Avalonia UI now, or even a website someday. 
   As long as whoever's calling BorrowAsync() gives it a student ID and equipment ID, 
   it works the same way regardless of what kind of interface is on top of it.

5. What advantage is gained from registering dependencies in one composition point?
   
   -Having everything registered in one place (App.axaml.cs, in ConfigureServices) 
   makes it way easier to see the whole picture of how the app is wired together, 
   instead of having new SomeRepository() scattered randomly across different files. 
   If we want to change something , like switching a repository to Transient instead of Singleton, 
   or swapping in a different implementation later, we only need to touch that one spot instead of hunting through the whole project.

6. If the in-memory repository were replaced by SQLite later, which parts of the current
   interface should remain largely unchanged?
  
  -Pretty much everything except the Infrastructure folder. 
   The Views, the ViewModels, and the Application services (BorrowEquipmentService, ReturnEquipmentService) 
   never actually mention "in-memory" anywhere in their code, they only use the repository interfaces. 
   So if we swapped in SQLite, we'd just need to create new repository classes like SqliteEquipmentRepository that 
   implement the same interfaces, and change the registration line in App.axaml.cs to point to those instead. 
   Everything else stays the same, which honestly is the whole point of doing it this way.