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



    Laboratory Activity 3 - SQLite and Entity Framework Core


1. Relational Database Design

The database has three tables that mirror the domain models from Activity 1. Students and
Equipment are both referenced by Borrowings through foreign keys, so a borrowing record always
points back to exactly one student and one equipment item instead of repeating their information.

STUDENTS
StudentId PK
Name
IsAllowedToBorrow

EQUIPMENT
EquipmentId PK
Name
IsAvailable

BORROWINGS
BorrowingId PK
StudentId FK
EquipmentId FK
DateBorrowed
ExpectedReturnDate
Status

Students to Borrowings is one to many, and Equipment to Borrowings is one to many as well. A
student can have several borrowing records over time, and a piece of equipment can appear in
several borrowing records too, but each borrowing only ever belongs to one student and one
equipment item. The diagram is saved at docs/database-diagram.png.

Name fields on Students and Equipment are required with a max length, and the two foreign keys
on Borrowings use restrict delete behavior so a student or equipment record can't be removed while
it still has borrowing history attached. Indexes were added on StudentId and EquipmentId inside
Borrowings since those are the columns we filter on most often.

2. SQLite and EF Core

We added the Microsoft.EntityFrameworkCore.Sqlite, Microsoft.EntityFrameworkCore.Design, and
Microsoft.EntityFrameworkCore.Tools packages to the Infrastructure project. A new Persistence
folder was created to hold the DbContext and the entity configuration classes, and the old
in-memory repositories were replaced with EF Core backed versions that still implement the same
interfaces from Activity 1.

3. DbContext

EquipmentBorrowingDbContext represents the database session for the whole application. It exposes
a DbSet for Students, Equipment, and Borrowings, and it applies all the entity configurations
automatically through OnModelCreating. It gets its connection string through its constructor the
same way everything else in this project receives its dependencies, so it never hardcodes where
the database file actually lives. It is only ever used inside the Infrastructure repositories and
inside App.axaml.cs, never inside a View or a ViewModel.

4. Repository Transition

Before this activity, IEquipmentRepository, IStudentRepository, and IBorrowingRepository were all
implemented by classes that stored everything in a plain C# List, which meant the data disappeared
the moment the app closed. Those were replaced with EfEquipmentRepository, EfStudentRepository,
and EfBorrowingRepository, which now read from and write to the SQLite database through the
DbContext. Since BorrowEquipmentService, ReturnEquipmentService, and every ViewModel only ever
depended on the repository interfaces and never on the concrete in-memory classes, none of that
code had to change at all for this swap to work.

5. Migration Process

The initial migration was created through the Package Manager Console with the default project set
to EquipmentBorrowing.Infrastructure, using

Add-Migration InitialCreate -Project EquipmentBorrowing.Infrastructure -StartupProject EquipmentBorrowing.Desktop

and then applied with

Update-Database -Project EquipmentBorrowing.Infrastructure -StartupProject EquipmentBorrowing.Desktop

The app also calls dbContext.Database.Migrate() automatically when it starts up, so the database
gets created and any pending migrations get applied without anyone needing to run those commands
manually.

6. Generated SQL

LINQ Query
Retrieve all equipment, used by EquipmentViewModel to populate the Equipment screen.

_context.Equipment.AsNoTracking().ToListAsync(cancellationToken);

Generated SQL

SELECT "e"."EquipmentId", "e"."Name", "e"."IsAvailable"
FROM "Equipment" AS "e"

Explanation
This just pulls every column from the Equipment table with no filtering at all, which matches the
plain LINQ call with no Where clause.

LINQ Query
Retrieve active borrowings, used by BorrowingsViewModel to populate the Active Borrowings screen.

_context.Borrowings.AsNoTracking().Where(b => b.Status == BorrowingStatus.Active).ToListAsync(cancellationToken);

Generated SQL

SELECT "b"."BorrowingId", "b"."DateBorrowed", "b"."EquipmentId", "b"."ExpectedReturnDate",
       "b"."StudentId", "b"."Status"
FROM "Borrowings" AS "b"
WHERE "b"."Status" = 0

Explanation
The Where clause in the LINQ query becomes an actual SQL WHERE clause here, so the filtering
happens inside SQLite itself instead of loading every borrowing into memory first and filtering
in C#. The 0 is the integer value EF Core stores for BorrowingStatus.Active.

Both of these were confirmed by temporarily enabling EF Core logging on the DbContext with LogTo
and watching the output while navigating between the Equipment and Active Borrowings screens.

7. Tracking Decisions

GetAllAsync and the active borrowings query both use AsNoTracking because they only exist to show
data on screen, nothing about them changes an entity afterward, so there's no reason for EF Core to
keep watching those objects for changes. The GetByIdAsync call that runs right before UpdateAsync
does not use AsNoTracking, because the equipment or borrowing returned from that call is about to
get modified (MarkAsBorrowed, MarkAsReturned) and EF Core needs to actually be tracking it to know
what changed once SaveChangesAsync runs.

8. Persistence Demonstration

We tested this by borrowing a piece of equipment, confirming it showed up right away on the Active
Borrowings screen, then fully closing the app and opening it again. The borrowing was still there
after restarting, which proved the data was actually coming from the SQLite file and not just
sitting in memory like it was in Activity 2. We repeated the same test for returning equipment,
closing and reopening the app again afterward, and the returned state was still correct too.

9. Architectural Reflection

1. Why did the application not need to be completely rewritten when SQLite was introduced?

-Because everything above Infrastructure only ever talked to the repository interfaces, not the
actual in-memory classes. So swapping what's behind those interfaces was basically the only
change that had to happen. Domain, Application, and the whole Desktop UI stayed exactly the same.

2. Why should the ViewModel not use DbContext directly?

-Same reason as not letting the View touch a repository directly back in Activity 2. If the
ViewModel used DbContext itself it would be tightly stuck to EF Core and SQLite specifically, and
it would also probably end up skipping the validation rules that are supposed to live in the
Application services. The ViewModel's job is to manage the screen, not to know how data gets saved.

3. What responsibility does the repository implementation now perform?

-It's the bridge that actually talks to the database. Before it was just reading and writing to a
List, now it translates the repository interface calls into real EF Core queries against
EquipmentBorrowingDbContext, which then get turned into actual SQL that runs against SQLite.

4. What is the purpose of an EF Core migration?

-It keeps a record of how the database schema should look and how it changed over time, so the
schema can be recreated or updated automatically instead of someone manually writing CREATE TABLE
statements by hand. It's basically version control for the database structure.

5. Why are foreign keys important in the borrowing database?

-They make sure a borrowing record can't point to a student or equipment item that doesn't
actually exist. Without them nothing would stop a bad StudentId or EquipmentId from being saved,
and the whole point of referencing instead of duplicating data falls apart if the reference isn't
actually enforced.

6. Why can a read only query benefit from AsNoTracking()?

-Because EF Core normally keeps track of every entity it loads in case you change it later, and
that tracking costs memory and a bit of performance. If we're only displaying the data and never
going to modify those exact objects, there's no reason to pay that cost, so AsNoTracking just skips
it and makes the query a little faster and lighter.

7. What would happen to the rest of the application if the SQLite implementation were replaced
   later by another database provider?

-Pretty much the same thing that happened when we moved from in-memory to SQLite in this activity.
Only the Infrastructure project would need new repository classes and a different connection setup
in App.axaml.cs, since everything else only depends on the repository interfaces and has no idea
SQLite was ever involved in the first place.