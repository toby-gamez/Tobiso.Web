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

### Relationships
- Category has self-referencing parent/child relationship.
- Post optionally belongs to a Category.

---

### Mermaid ER Diagram

```mermaid
erDiagram
	CATEGORY {
		int Id PK
		string Name
		string Slug
		int ParentId FK
	}
	POST {
		int Id PK
		string Title
		string Content
		string FilePath
		datetime CreatedAt
		datetime UpdatedAt
		int CategoryId FK
	}
	QUESTION {
		int Id PK
		nvarchar(200) Question
		int PostId FK
	}
	ANSWER {
		int Id PK
		nvarchar(200) AnswerText
		int QuestionId FK
	}
	CATEGORY ||--o| CATEGORY : Parent
	CATEGORY ||--o| POST : "Has Posts"
	POST }o--|| CATEGORY : "Belongs to"
	QUESTION }o--|| POST : "Relates to"
	ANSWER }o--|| QUESTION : "Possible Answers"

```
