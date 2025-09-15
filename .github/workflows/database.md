## Tobiso.Web Database Documentation

### Entities

#### Category
- `Id` (int, PK)
- `Name` (nvarchar(128), required)
- `Slug` (nvarchar(128), required)
- `ParentId` (int, FK, nullable) → references `Category(Id)`
- `Children` (List<Category>)

#### Post
- `Id` (int, PK)
- `Title` (nvarchar, required)
- `Content` (nvarchar, required)
- `FilePath` (nvarchar, required)
- `CreatedAt` (datetime2, required)
- `UpdatedAt` (datetime2, nullable)
- `CategoryId` (int, FK, nullable) → references `Category(Id)`

#### Question
- `Id` (int, PK)
- `QuestionText` (nvarchar(200), required)
- `PostId` (int, FK, required) → references `Post(Id)`

#### Answer
- `Id` (int, PK)
- `AnswerText` (nvarchar(200), required)
- `Correct` (int, required)
- `QuestionId` (int, FK, required) → references `Question(Id)`

#### Explanation
- `Id` (int, PK)
- `Text` (nvarchar(500), required)
- `QuestionId` (int, FK, required) → references `Question(Id)`

### Relationships
- Category has self-referencing parent/child relationship.
- Post optionally belongs to a Category.
- Question is related to a Post.
- Answer belongs to a Question.
- Explanation belongs to a Question.

---

### Mermaid ER Diagram

```mermaid
erDiagram
	Category {
		int Id PK
		string Name
		string Slug
		int ParentId FK
	}
	Post {
		int Id PK
		string Title
		string Content
		string FilePath
		datetime CreatedAt
		datetime UpdatedAt
		int CategoryId FK
	}
	Question {
		int Id PK
		nvarchar(200) Question
		int PostId FK
	}
	Answer {
		int Id PK
		nvarchar(200) AnswerText
		int QuestionId FK
	}
	Explanation {
		int Id PK
		nvarchar(500) Text
		int QuestionId FK
	}
	Category ||--o| Category : Parent
	Category ||--o| Post : "Has Posts"
	Post }o--|| Category : "Belongs to"
	Question }o--|| Post : "Relates to"
	Answer }o--|| Question : "Possible Answers"
	Explanation }o--|| Question : "Contains Explanations"

```

---

### SQL for New Tables

```sql
CREATE TABLE [Questions] (
	[Id] INT PRIMARY KEY IDENTITY(1,1),
	[Question] NVARCHAR(200) NOT NULL,
	[PostId] INT NOT NULL,
	FOREIGN KEY ([PostId]) REFERENCES [Post]([Id])
);

CREATE TABLE [Answers] (
	[Id] INT PRIMARY KEY IDENTITY(1,1),
	[AnswerText] NVARCHAR(200) NOT NULL,
	[Correct] INT NOT NULL,
	[QuestionId] INT NOT NULL,
	FOREIGN KEY ([QuestionId]) REFERENCES [Questions]([Id])
);

CREATE TABLE [Explanations] (
	[Id] INT PRIMARY KEY IDENTITY(1,1),
	[Text] NVARCHAR(500) NOT NULL,
	[QuestionId] INT NOT NULL,
	FOREIGN KEY ([QuestionId]) REFERENCES [Questions]([Id])
);
```
