# Architecture - מערכת Idempotency לדואר

## Overview
מערכת דמו מתקדמת להדגמת דפוסי POST request idempotency במערכות דואר, המונעת כפילויות משלוחים וחיובים מיותרים.

## System Architecture Diagram

```mermaid
graph TB
    subgraph "Client Layer"
        UI[Angular 20 Frontend<br/>ממשק משתמש בעברית]
        Mobile[Mobile Apps<br/>אפליקציות נייד]
        API_Client[External API Clients<br/>לקוחות API חיצוניים]
    end

    subgraph "API Gateway Layer"
        Gateway[API Gateway<br/>שער API]
        LB[Load Balancer<br/>מאזן עומסים]
    end

    subgraph "Application Layer"
        subgraph "Web API (.NET 8)"
            Controller[ShipmentsController<br/>בקר משלוחים]
            Middleware[Middleware Pipeline<br/>צינור עיבוד בקשות]
            
            subgraph "Middleware Components"
                CorrId[CorrelationId<br/>מזהה מעקב]
                Chaos[Chaos Engineering<br/>הנדסת כאוס]
                Auth[Authentication<br/>אימות]
            end
        end
        
        subgraph "Business Services"
            IdempotencyService[Idempotency Service<br/>שירות אי-כפילות]
            ShipmentService[Shipment Service<br/>שירות משלוחים]
            ChaosService[Chaos Service<br/>שירות כאוס]
        end
    end

    subgraph "Data Layer"
        subgraph "PostgreSQL Database"
            ShipmentsTable[(Shipments Table<br/>טבלת משלוחים)]
            IdempotencyTable[(IdempotencyEntries Table<br/>טבלת מפתחות אי-כפילות)]
        end
        
        Cache[Redis Cache<br/>מטמון מהיר]
    end

    subgraph "Infrastructure"
        Logs[Structured Logging<br/>לוגים מובנים]
        Monitoring[Monitoring & Metrics<br/>ניטור ומדדים]
        Docker[Docker Containers<br/>מכולות דוקר]
    end

    %% Connections
    UI --> Gateway
    Mobile --> Gateway
    API_Client --> Gateway
    
    Gateway --> LB
    LB --> Controller
    
    Controller --> Middleware
    Middleware --> CorrId
    Middleware --> Chaos
    Middleware --> Auth
    
    Controller --> IdempotencyService
    Controller --> ShipmentService
    Controller --> ChaosService
    
    IdempotencyService --> IdempotencyTable
    ShipmentService --> ShipmentsTable
    
    IdempotencyService --> Cache
    
    Controller --> Logs
    Controller --> Monitoring
    
    %% Styling
    classDef frontend fill:#e1f5fe
    classDef api fill:#f3e5f5
    classDef service fill:#e8f5e8
    classDef data fill:#fff3e0
    classDef infra fill:#fce4ec
    
    class UI,Mobile,API_Client frontend
    class Controller,Middleware,CorrId,Chaos,Auth api
    class IdempotencyService,ShipmentService,ChaosService service
    class ShipmentsTable,IdempotencyTable,Cache data
    class Logs,Monitoring,Docker infra
```

## Idempotency Flow Diagram

```mermaid
sequenceDiagram
    participant Client as לקוח
    participant API as API Server
    participant IdempSvc as Idempotency Service
    participant DB as Database
    participant Cache as Cache

    Note over Client,Cache: תרחיש 1: בקשה חדשה
    Client->>API: POST /shipments + Idempotency-Key
    API->>IdempSvc: בדיקת מפתח קיים
    IdempSvc->>DB: SELECT FROM IdempotencyEntries
    DB-->>IdempSvc: לא נמצא
    IdempSvc-->>API: מפתח חדש
    API->>API: עיבוד בקשה
    API->>DB: שמירת משלוח
    API->>IdempSvc: שמירת תוצאה
    IdempSvc->>DB: INSERT IdempotencyEntry
    IdempSvc->>Cache: שמירה במטמון
    API-->>Client: 200 OK + תוצאה

    Note over Client,Cache: תרחיש 2: בקשה חוזרת (אותו תוכן)
    Client->>API: POST /shipments + אותו Idempotency-Key
    API->>IdempSvc: בדיקת מפתח קיים
    IdempSvc->>Cache: בדיקה במטמון
    Cache-->>IdempSvc: נמצא
    IdempSvc-->>API: תוצאה שמורה
    API-->>Client: 200 OK + X-Idempotency-Replayed: true

    Note over Client,Cache: תרחיש 3: בקשה חוזרת (תוכן שונה)
    Client->>API: POST /shipments + אותו מפתח, תוכן שונה
    API->>IdempSvc: בדיקת hash תוכן
    IdempSvc->>DB: השוואת RequestHash
    DB-->>IdempSvc: hash שונה!
    IdempSvc-->>API: קונפליקט
    API-->>Client: 409 Conflict
```

## Component Architecture

```mermaid
graph LR
    subgraph "Frontend Components"
        CreateForm[Create Shipment Form<br/>טופס יצירת משלוח]
        CancelForm[Cancel Form<br/>טופס ביטול]
        StatusForm[Status Update Form<br/>טופס עדכון סטטוס]
        LookupForm[Lookup Form<br/>טופס חיפוש]
        OperationsLog[Operations Log<br/>יומן פעולות]
        ChaosPanel[Chaos Control Panel<br/>פאנל בקרת כאוס]
    end

    subgraph "API Services"
        PostalAPI[Postal API Service<br/>שירות API דואר]
        ChaosAPI[Chaos API Service<br/>שירות API כאוס]
    end

    subgraph "Backend Controllers"
        ShipmentCtrl[Shipments Controller<br/>בקר משלוחים]
        HealthCtrl[Health Controller<br/>בקר בריאות]
    end

    subgraph "Core Services"
        IdempSvc[Idempotency Service<br/>שירות אי-כפילות]
        ShipSvc[Shipment Service<br/>שירות משלוחים]
        ChaosSvc[Chaos Service<br/>שירות כאוס]
    end

    CreateForm --> PostalAPI
    CancelForm --> PostalAPI
    StatusForm --> PostalAPI
    LookupForm --> PostalAPI
    ChaosPanel --> ChaosAPI
    
    PostalAPI --> ShipmentCtrl
    ChaosAPI --> ShipmentCtrl
    
    ShipmentCtrl --> IdempSvc
    ShipmentCtrl --> ShipSvc
    ShipmentCtrl --> ChaosSvc
```

## Data Architecture

```mermaid
erDiagram
    SHIPMENTS {
        uuid id PK
        varchar barcode UK       
        varchar customer_name
        varchar address
        decimal weight
        decimal price
        int status
        timestamp created_at
        timestamp updated_at
        varchar notes
    }

    IDEMPOTENCY_ENTRIES {
        varchar idempotency_key PK
        varchar request_hash
        text response_body
        int status_code
        timestamp created_at
        timestamp expires_at
        varchar request_path
        varchar operation
        uuid related_entity_id FK
    }

    OPERATION_LOGS {
        uuid id PK
        varchar operation_type
        varchar idempotency_key
        int status_code
        boolean is_replayed
        timestamp timestamp
        text request_data
        text response_data
        varchar error_message
        int duration_ms
    }

    SHIPMENTS ||--o{ IDEMPOTENCY_ENTRIES : "relates to"
    IDEMPOTENCY_ENTRIES ||--o{ OPERATION_LOGS : "tracks"
```

## Technology Stack

### Frontend (Angular 20)
- **Framework**: Angular 20 עם TypeScript
- **UI Library**: Angular Material Design
- **State Management**: RxJS Observables
- **HTTP Client**: Angular HttpClient עם Interceptors
- **Styling**: SCSS עם RTL support

### Backend (.NET 8)
- **Framework**: ASP.NET Core 8 Web API
- **ORM**: Entity Framework Core 8
- **Database**: PostgreSQL 15
- **Logging**: Serilog עם structured logging
- **Documentation**: OpenAPI/Swagger
- **Caching**: Redis (אופציונלי)

### Infrastructure
- **Containerization**: Docker & Docker Compose
- **Database**: PostgreSQL או Supabase
- **Monitoring**: Built-in health checks
- **CI/CD**: GitHub Actions (עתידי)

## Security Considerations

### Idempotency Security
- **Key Validation**: בדיקת תקינות מפתחות
- **Hash Verification**: SHA256 לאימות תוכן
- **TTL Management**: מחיקה אוטומטית של entries ישנים
- **Rate Limiting**: הגבלת קצב בקשות

### API Security
- **CORS Configuration**: הגדרות CORS מתאימות
- **Input Validation**: בדיקת קלט בכל השכבות
- **Error Handling**: טיפול מאובטח בשגיאות
- **Correlation Tracking**: מעקב בקשות עם IDs

## Scalability & Performance

### Horizontal Scaling
- **Stateless API**: שרת API ללא state
- **Database Sharding**: חלוקת נתונים (עתידי)
- **Load Balancing**: איזון עומסים
- **Caching Strategy**: אסטרטגיית מטמון

### Performance Optimizations
- **Connection Pooling**: מאגר חיבורי DB
- **Async Operations**: פעולות אסינכרוניות
- **Index Optimization**: אופטימיזציה של אינדקסים
- **Memory Management**: ניהול זיכרון יעיל

## Monitoring & Observability

### Logging Strategy
- **Structured Logs**: לוגים מובנים עם JSON
- **Correlation IDs**: מעקב בקשות חוצות מערכות
- **Performance Metrics**: מדדי ביצועים
- **Error Tracking**: מעקב שגיאות מפורט

### Health Monitoring
- **Health Checks**: בדיקות בריאות מערכת
- **Database Connectivity**: בדיקת חיבור DB
- **External Dependencies**: בדיקת תלויות חיצוניות
- **Resource Usage**: ניטור שימוש במשאבים
