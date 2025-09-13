# jpPostIdempotencyDemo

A demonstration project for POST request idempotency patterns.

MCP OK 

## 📮 הדגמת אידמפוטנטיות דואר - Postal Idempotency Demo

מערכת מתקדמת להדגמת דפוסי אידמפוטנטיות בפעולות דואר, הכוללת API בנוי על .NET 8, ממשק משתמש ב-Angular 20, ומסד נתונים PostgreSQL/Supabase.

## 🎯 **מטרת הפרויקט**

פתרון בעיות נפוצות במערכות דואר:
- **כפילות משלוחים** - מניעת יצירת משלוחים זהים
- **חיוב כפול** - הבטחת חיוב יחיד למשלוח
- **הדפסת תוויות מיותרת** - חיסכון בעלויות הדפסה
- **אי-עקביות נתונים** - שמירה על שלמות המידע

## 🚀 **התחלה מהירה**

### **דרישות מערכת**
- **.NET 8 SDK**
- **Node.js 18+** עם npm
- **Docker** (לפיתוח מקומי)
- **PostgreSQL** או חשבון **Supabase**

### **1. שכפול הפרויקט**
```bash
git clone https://github.com/your-username/jpPostIdempotencyDemo.git
cd jpPostIdempotencyDemo
```

### **2. הרצת מסד הנתונים**

#### **אפשרות A: Docker (מומלץ לפיתוח)**
```bash
cd database
docker-compose up -d
```

#### **אפשרות B: Supabase**
1. צור פרויקט ב-[Supabase Dashboard](https://app.supabase.com)
2. הרץ את `database/supabase/schema.sql` ב-SQL Editor
3. עדכן את מחרוזת החיבור ב-`appsettings.json`

### **3. הרצת Backend API**
```bash
cd src/backend/PostalIdempotencyDemo.Api
dotnet restore
dotnet run
```
API יהיה זמין ב: https://localhost:7001

### **4. הרצת Frontend**
```bash
cd src/frontend/postal-idempotency-app
npm install
ng serve
```
האפליקציה תהיה זמינה ב: http://localhost:4200

## 📊 **מבנה הפרויקט**

```
jpPostIdempotencyDemo/
├── src/
│   ├── backend/PostalIdempotencyDemo.Api/     # .NET 8 API
│   │   ├── Controllers/                       # API Controllers
│   │   ├── Services/                          # Business Logic
│   │   ├── Repositories/                      # Data Access (ADO.NET)
│   │   ├── Models/                            # Data Models
│   │   ├── DTOs/                              # Data Transfer Objects
│   │   └── Middleware/                        # Custom Middleware
│   └── frontend/postal-idempotency-app/       # Angular 20 App
│       ├── src/app/components/                # UI Components
│       ├── src/app/services/                  # Angular Services
│       └── src/app/models/                    # TypeScript Models
├── database/
│   ├── schema.sql                             # PostgreSQL Schema
│   ├── docker-compose.yml                    # Docker Setup
│   └── supabase/                              # Supabase Setup
├── docs/                                      # Documentation
└── tasks/                                     # Development Tasks
```

## 🔧 **תכונות עיקריות**

### **🛡️ אידמפוטנטיות מתקדמת**
- **SHA256 hashing** של תוכן הבקשה
- **TTL configurable** לרשומות
- **Conflict detection** עם HTTP 409
- **Response caching** לביצועים

### **🎭 Chaos Engineering**
- **Network delays** סימולציה
- **Random failures** בדיקת עמידות
- **Configurable rates** שליטה מלאה
- **Real-time control** דרך UI

### **📱 ממשק משתמש מתקדם**
- **Material Design** עם תמיכה בעברית
- **RTL Support** מלא
- **Reactive Forms** עם ולידציה
- **Real-time feedback** עם snackbars

## 🧪 **בדיקת המערכת**

### **1. בדיקת אידמפוטנטיות**
1. פתח את האפליקציה ב-http://localhost:4200
2. עבור ל"יצירת משלוח"
3. מלא פרטי משלוח
4. לחץ על "בדיקת אידמפוטנטיות" - ישלח 3 בקשות זהות
5. צפה בתגובות: הראשונה 201, השאר 200

### **2. בדיקת Chaos Engineering**
1. עבור ל"בקרת כאוס"
2. הפעל כאוס עם הגדרות קלות
3. נסה ליצור משלוח - צפה בעיכובים
4. הגבר את הגדרות הכאוס לבדיקת כשלים

### **3. בדיקת ניהול משלוחים**
1. עבור ל"חיפוש משלוח"
2. חפש לפי ברקוד: `1234567890123`
3. נסה לבטל או לעדכן סטטוס
4. צפה בהגבלות לפי מצב המשלוח

## 🔗 **API Endpoints**

### **משלוחים**
- `POST /api/shipments` - יצירת משלוח חדש
- `GET /api/shipments/{barcode}` - קבלת משלוח לפי ברקוד
- `PUT /api/shipments/{barcode}/cancel` - ביטול משלוח
- `PUT /api/shipments/{barcode}/status` - עדכון סטטוס

### **Chaos Engineering**
- `POST /api/shipments?chaos=true` - הפעלת כאוס
- `POST /api/shipments?delay=1000` - עיכוב מותאם
- `POST /api/shipments?failure_rate=0.3` - שיעור כשלים

## 🗄️ **מסד נתונים**

### **טבלת shipments**
- `id` - מזהה ייחודי (UUID)
- `barcode` - ברקוד ייחודי למשלוח
- `kod_peula` - קוד פעולה במערכת הדואר
- `status` - סטטוס (1=נוצר, 2=בדרך, 3=נמסר, 4=בוטל, 5=נכשל)

### **טבלת idempotency_entries**
- `idempotency_key` - מפתח אידמפוטנטיות (SHA256)
- `request_hash` - Hash של תוכן הבקשה
- `response_body` - תוכן התגובה המקורית
- `expires_at` - תאריך תפוגה לרשומה

## 🔒 **אבטחה**

- **Parameterized queries** מניעת SQL Injection
- **Input validation** בכל השכבות
- **CORS configuration** מוגבל
- **RLS policies** ב-Supabase

## 📈 **ניטור ולוגים**

- **Serilog** עם structured logging
- **Correlation IDs** למעקב בקשות
- **Health checks** לבדיקת מערכת

## 🤝 **תרומה לפרויקט**

1. Fork הפרויקט
2. צור branch חדש
3. Commit השינויים
4. Push ל-branch
5. פתח Pull Request

## 📄 **רישיון**

פרויקט זה מופץ תחת רישיון MIT.
