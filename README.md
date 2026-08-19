# PlayerCards 

A full-stack **ASP.NET Core MVC** web application for a football (soccer) merchandise store, featuring a complete customer facing storefront and a powerful multi-role administration panel. Built with C#, Entity Framework Core, and Bootstrap, with full English/Arabic localization.

---

###  Customer Storefront
- **Branded shop landing page** with hero banner and featured collections (FC Barcelona, Real Madrid, and more)
- **Product catalog** of player cards / kits with images, categories, tags, and pricing
- **Wishlist** — like/unlike items and view your saved favourites
- **Shopping cart** — add and remove items
- **Category filtering** and product search
- **Responsive design** that works across desktop and mobile

---

###  Admin Panel (SuperAdmin)
- **User Management** — create, edit, activate/deactivate, and search users with pagination
- **Role management** — promote users to Admin / SuperAdmin
- **Item card management** — add, edit, and delete product cards with image uploads
- **Category management** — create and delete product categories
- **Tag system** — assign tags to items in bulk
- **Announcements** — send rich-text announcements to selected users
- **Bulk user import** — import users from an Excel spreadsheet (via ClosedXML)
- **Analytics views** — "Most Liked" and "Most In Cart" items

---

###  Platform
- **Multi-language support** — English & Arabic (RTL) via ASP.NET Core Localization
- **Role-based access** — SuperAdmin, Admin, and User roles
- **Cookie authentication** with session management
- **Password reset** flow with secure token generation and email delivery

---

##  Notes on This Build

This is a **portfolio-oriented version** of the application:
- The original SQL Server dependency has been replaced with an **in-memory database** so it runs anywhere with zero configuration.
- Data is **not persisted** between runs — every launch starts with fresh seed data.

---

##  License

This project is provided for educational and portfolio demonstration purposes.

---

##  Author

**Rakan A Salah**
Amman, Jordan
