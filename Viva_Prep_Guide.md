# C# Viva Preparation Guide - Journal App

This guide summarizes core C# and OOP concepts used in your project to help you prepare for your viva.

## 1. OOP Concepts in Your Project

### **Class & Object**
- **Definition**: A **Class** is a blueprint (e.g., `JournalEntry`), and an **Object** is an instance of that class (e.g., a specific diary entry saved to the DB).
- **In Your Project**: Files in the `Models` folder (like `User.cs`, `JournalEntry.cs`) define the "shape" of your data.

### **The "Shape" Metaphor (OOP Basics)**
- **Concept**: Teachers often ask about a `Shape` class with `Circle` and `Square` subclasses to explain inheritance and polymorphism. In your project, `JournalEntry` is like a "Shape," and the specific data (with Tags or Moods) defines its specific properties.

### **Abstraction**
- **Definition**: Hiding complex implementation details and showing only necessary features.
- **In Your Project**: The `JournalService` abstracts the database queries. When you call `SaveTodayEntryAsync()`, you don't need to know *how* the SQL query is written; you just provide the title and content.

### **Inheritance**
- **Definition**: Allowing a class to inherit properties and methods from another class.
- **In Your Project**: `MainLayout.razor` inherits from `LayoutComponentBase`. This gives your layout all the standard Blazor layout features automatically.

### **Encapsulation (Access Modifiers)**
- **Definition**: Restricting direct access up to some of an object's components. It's about "hiding" data inside a class.
- **In Your Project**: In `AuthService.cs`, `_currentUser` is **private** (hidden from outside), but we provide a **public property** `CurrentUser` to allow read-only access.
- **Access Modifiers**:
    - `public`: Accessible from anywhere.
    - `private`: Accessible only within the same class.
    - `protected`: Accessible within the same class and its subclasses.
    - `readonly`: Ensures a field can only be assigned during initialization.

### **Polymorphism (Override)**
- **Definition**: Providing a specific implementation of a method that is already defined in its base class.
- **In Your Project**: In `MainLayout.razor`, we use `protected override async Task OnAfterRenderAsync(bool firstRender)`. We are *overriding* the default Blazor lifecycle method to add our theme-loading logic.

### **Interface**
- **Definition**: A contract that defines *what* a class should do, but not *how*.
- **Note**: While your current project uses concrete classes (like `JournalService`), interfaces (e.g., `IJournalService`) are often used in larger projects to make testing easier.

---

## 2. Core C# Concepts

### **Dependency Injection (DI)**
- **What it is**: A design pattern where objects receive their dependencies from an external source rather than creating them.
- **In Your Project**: Look at `MauiProgram.cs`. We use `builder.Services.AddSingleton<JournalService>()`. This tells the app to create one instance of the service and "inject" it wherever it's needed (using `@inject` in `.razor` files or constructor injection in other services).

### **LINQ (Language Integrated Query)**
- **What it is**: A powerful way to query data directly using C# syntax.
- **In Your Project**: In `JournalDatabase.cs`, you use LINQ to filter entries:
  ```csharp
  var entries = await _database.Table<JournalEntry>()
      .Where(e => e.UserId == userId)
      .OrderByDescending(e => e.EntryDate)
      .ToListAsync();
  ```

### **Generics**
- **Definition**: Allows you to write a class or method that can work with any data type.
- **In Your Project**: Look at `JournalDatabase.cs`. Methods like `_database.CreateTableAsync<User>()` use **Generics** (the `<T>` syntax). This allows one method to create tables for any of your models (User, Entry, Tag, etc.) without writing a separate method for each.

### **Delegation & Binding**
- **Delegation**: In Blazor, this happens via `EventCallback`. For example, `@onclick="HandleLogin"` delegates the click event to your C# method.
- **@bind**: This is "Two-Way Data Binding." In `Login.razor`, `@bind="email"` means if the user types in the box, the C# variable updates, and if the C# variable changes, the box updates.

### **Async/Await**
- **Purpose**: Prevents the UI from freezing while waiting for slow operations (like database saving). Every DB call in your project uses `async Task` to keep the app responsive.

---

## 3. Project-Specific Implementation

### **Database Connection**
- **Technology**: SQLite (using the `sqlite-net-pcl` library).
- **Connection**: In `JournalDatabase.cs`, `InitAsync()` creates a connection to a file named `journals.db3` stored in the app's local data folder.

### **Pagination**
- **How it's done**: Using the `.Skip()` and `.Take()` methods in `JournalDatabase.cs`.
  - `Skip(skip)`: Ignores the first 'X' entries.
  - `Take(take)`: Grabs the next 'Y' entries.
- **Use case**: Loading only 10 entries at a time on the "All Entries" page instead of loading thousands at once.

### **Error Handling**
- **Implementation**: We use `try-catch` blocks.
- **Example**: In `JournalService.SaveTodayEntryAsync`, any database errors are caught, and a friendly message like `$"Error: {ex.Message}"` is returned to the UI instead of crashing the app.

---

## 4. Possible Teacher Questions & Answers

**Q: Why did you use Singletons for your services in MauiProgram?**
**A**: Because services like `AuthService` and `JournalDatabase` need to maintain their state (like the current logged-in user or the DB connection) throughout the entire app session. One instance is sufficient and memory-efficient.

**Q: How do you ensure user privacy in the database?**
**A**: Every database query (see `GetAllEntriesAsync(userId)`) strictly filters by the `UserId` of the currently logged-in user. This ensures one user cannot see another user's journal entries.

**Q: What is the benefit of the MVVM/Service pattern you used?**
**A**: It separates the UI (Blazor components) from the Data logic (SQLite/Services). This makes the code cleaner, easier to debug, and allows us to change the database in the future without rebuilding the UI.

**Q: How do you handle security for user passwords?**
**A**: We use **SHA256 hashing**. In `AuthService.cs`, we never store the actual password. We convert it into a hash string and store that. When a user logs in, we hash their input and compare it to the stored hash. This way, even if someone steals the database, they don't have the passwords.

**Q: What is the difference between a Field and a Property?**
**A**: A **Field** (like `private User? _currentUser`) is a variable declared directly in a class. A **Property** (like `public User? CurrentUser => _currentUser`) is a member that provides a flexible mechanism to read or write the value of a private field.

**Q: How does your export feature work on mobile/Mac?**
**A**: Since file systems are restricted, we use `Share.Default.RequestAsync` from the MAUI Essentials library. This saves the PDF to a temporary cache directory and then opens the native system share dialog so the user can choose where to save or send the file.

**Q: Explain the use of "Value Types" versus "Reference Types" in your models.**
**A**: `int` (like `UserId`) is a **Value Type**; it holds the data directly. Most of our custom models (like `JournalEntry`) are **Reference Types** (classes); the variable holds a "pointer" to where the object lives in memory.

---

## 5. Additional Key Concepts

### **Namespaces & Naming**
- **Namespaces**: Used to organize code and prevent name conflicts (e.g., `namespace Journal.Services`).
- **Naming Conventions**: In C#, we use **PascalCase** for classes and methods (e.g., `JournalService`, `GetAllEntriesAsync`) and **camelCase** for local variables (e.g., `userId`).

### **C# Collections**
- **List<T>**: Used throughout the project (e.g., `List<JournalEntry>`) for dynamic arrays of data.
- **Any(), Min(), Max()**: These are **LINQ extension methods** used in `ExportService.cs` to quickly find statistics about your journal entries.
sqlite3 ~/Library/journals.db3
