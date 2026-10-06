-- ==============================================================================
-- CƠ SỞ DỮ LIỆU: WebBanCameraGiamSat (Nhom 8 - CamPro)
-- HỆ QUẢN TRỊ CSDL: Microsoft SQL Server (T-SQL)
-- ==============================================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'WebBanCameraGiamSat')
BEGIN
    CREATE DATABASE WebBanCameraGiamSat;
END
GO

USE WebBanCameraGiamSat;
GO

-- 1. BẢNG DANH MỤC (Categories)
IF OBJECT_ID(N'dbo.Categories', N'U') IS NULL
CREATE TABLE dbo.Categories (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name NVARCHAR(150) NOT NULL,
    Slug NVARCHAR(150) NOT NULL CONSTRAINT UQ_Categories_Slug UNIQUE,
    Description NVARCHAR(MAX) NULL,
    ImageUrl NVARCHAR(500) NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- 2. BẢNG THƯƠNG HIỆU (Brands)
IF OBJECT_ID(N'dbo.Brands', N'U') IS NULL
CREATE TABLE dbo.Brands (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Brands PRIMARY KEY,
    Name NVARCHAR(100) NOT NULL,
    Slug NVARCHAR(100) NOT NULL CONSTRAINT UQ_Brands_Slug UNIQUE,
    LogoUrl NVARCHAR(500) NULL,
    Description NVARCHAR(MAX) NULL,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- 3. BẢNG NGƯỜI DÙNG (AspNetUsers)
IF OBJECT_ID(N'dbo.AspNetUsers', N'U') IS NULL
CREATE TABLE dbo.AspNetUsers (
    Id NVARCHAR(450) NOT NULL CONSTRAINT PK_AspNetUsers PRIMARY KEY,
    UserName NVARCHAR(256) NULL,
    NormalizedUserName NVARCHAR(256) NULL,
    Email NVARCHAR(256) NULL,
    NormalizedEmail NVARCHAR(256) NULL,
    EmailConfirmed BIT NOT NULL DEFAULT 0,
    PasswordHash NVARCHAR(MAX) NULL,
    SecurityStamp NVARCHAR(MAX) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL,
    PhoneNumber NVARCHAR(50) NULL,
    PhoneNumberConfirmed BIT NOT NULL DEFAULT 0,
    TwoFactorEnabled BIT NOT NULL DEFAULT 0,
    LockoutEnd DATETIMEOFFSET NULL,
    LockoutEnabled BIT NOT NULL DEFAULT 0,
    AccessFailedCount INT NOT NULL DEFAULT 0,
    FullName NVARCHAR(150) NOT NULL DEFAULT N'',
    Address NVARCHAR(500) NULL,
    AvatarUrl NVARCHAR(500) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsActive BIT NOT NULL DEFAULT 1
);
CREATE UNIQUE INDEX UserNameIndex ON dbo.AspNetUsers (NormalizedUserName) WHERE NormalizedUserName IS NOT NULL;
CREATE INDEX EmailIndex ON dbo.AspNetUsers (NormalizedEmail);
GO

-- 4. BẢNG VAI TRÒ (AspNetRoles)
IF OBJECT_ID(N'dbo.AspNetRoles', N'U') IS NULL
CREATE TABLE dbo.AspNetRoles (
    Id NVARCHAR(450) NOT NULL CONSTRAINT PK_AspNetRoles PRIMARY KEY,
    Name NVARCHAR(256) NULL,
    NormalizedName NVARCHAR(256) NULL,
    ConcurrencyStamp NVARCHAR(MAX) NULL
);
CREATE UNIQUE INDEX RoleNameIndex ON dbo.AspNetRoles (NormalizedName) WHERE NormalizedName IS NOT NULL;
GO

-- 5. BẢNG PHÂN QUYỀN VAI TRÒ - NGƯỜI DÙNG (AspNetUserRoles)
IF OBJECT_ID(N'dbo.AspNetUserRoles', N'U') IS NULL
CREATE TABLE dbo.AspNetUserRoles (
    UserId NVARCHAR(450) NOT NULL,
    RoleId NVARCHAR(450) NOT NULL,
    CONSTRAINT PK_AspNetUserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_AspNetUserRoles_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_AspNetUserRoles_AspNetRoles FOREIGN KEY (RoleId) REFERENCES dbo.AspNetRoles (Id) ON DELETE CASCADE
);
GO

-- 6. BẢNG SẢN PHẨM (Products)
IF OBJECT_ID(N'dbo.Products', N'U') IS NULL
CREATE TABLE dbo.Products (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Products PRIMARY KEY,
    Name NVARCHAR(250) NOT NULL,
    Slug NVARCHAR(250) NOT NULL CONSTRAINT UQ_Products_Slug UNIQUE,
    CategoryId INT NOT NULL,
    BrandId INT NOT NULL,
    SKU NVARCHAR(100) NOT NULL CONSTRAINT UQ_Products_SKU UNIQUE,
    ShortDescription NVARCHAR(500) NULL,
    Description NVARCHAR(MAX) NULL,
    Price DECIMAL(18,0) NOT NULL,
    DiscountPrice DECIMAL(18,0) NULL,
    Stock INT NOT NULL DEFAULT 0,
    SoldCount INT NOT NULL DEFAULT 0,
    ViewCount INT NOT NULL DEFAULT 0,
    MainImageUrl NVARCHAR(500) NULL,
    Resolution NVARCHAR(100) NULL,
    ConnectionType NVARCHAR(100) NULL,
    InstallLocation NVARCHAR(100) NULL,
    NightVisionRange NVARCHAR(100) NULL,
    StorageType NVARCHAR(100) NULL,
    WarrantyMonths INT NOT NULL DEFAULT 24,
    IsFeatured BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_Products_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories (Id),
    CONSTRAINT FK_Products_Brands FOREIGN KEY (BrandId) REFERENCES dbo.Brands (Id)
);
GO

-- 7. BẢNG ẢNH SẢN PHẨM (ProductImages)
IF OBJECT_ID(N'dbo.ProductImages', N'U') IS NULL
CREATE TABLE dbo.ProductImages (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ProductImages PRIMARY KEY,
    ProductId INT NOT NULL,
    ImageUrl NVARCHAR(500) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    CONSTRAINT FK_ProductImages_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE
);
GO

-- 8. BẢNG GIỎ HÀNG (CartItems)
IF OBJECT_ID(N'dbo.CartItems', N'U') IS NULL
CREATE TABLE dbo.CartItems (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_CartItems PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    ProductId INT NOT NULL,
    Quantity INT NOT NULL DEFAULT 1,
    AddedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_CartItems_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_CartItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE
);
GO

-- 9. BẢNG MÃ GIẢM GIÁ (Vouchers)
IF OBJECT_ID(N'dbo.Vouchers', N'U') IS NULL
CREATE TABLE dbo.Vouchers (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Vouchers PRIMARY KEY,
    Code NVARCHAR(50) NOT NULL CONSTRAINT UQ_Vouchers_Code UNIQUE,
    DiscountAmount DECIMAL(18,0) NOT NULL DEFAULT 0,
    DiscountPercent INT NOT NULL DEFAULT 0,
    MinOrderAmount DECIMAL(18,0) NOT NULL DEFAULT 0,
    MaxDiscountAmount DECIMAL(18,0) NULL,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    UsageLimit INT NOT NULL DEFAULT 100,
    UsedCount INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- 10. BẢNG ĐƠN HÀNG (Orders)
IF OBJECT_ID(N'dbo.Orders', N'U') IS NULL
CREATE TABLE dbo.Orders (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    OrderCode NVARCHAR(50) NOT NULL CONSTRAINT UQ_Orders_OrderCode UNIQUE,
    UserId NVARCHAR(450) NOT NULL,
    OrderDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ReceiverName NVARCHAR(150) NOT NULL,
    ReceiverPhone NVARCHAR(50) NOT NULL,
    ShippingAddress NVARCHAR(500) NOT NULL,
    Note NVARCHAR(500) NULL,
    SubTotal DECIMAL(18,0) NOT NULL,
    ShippingFee DECIMAL(18,0) NOT NULL DEFAULT 0,
    DiscountAmount DECIMAL(18,0) NOT NULL DEFAULT 0,
    TotalAmount DECIMAL(18,0) NOT NULL,
    Status INT NOT NULL DEFAULT 0,
    PaymentMethod INT NOT NULL DEFAULT 0,
    PaymentStatus INT NOT NULL DEFAULT 0,
    VoucherCode NVARCHAR(50) NULL,
    TransactionId NVARCHAR(100) NULL,
    UpdatedDate DATETIME2 NULL,
    CONSTRAINT FK_Orders_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id)
);
GO

-- 11. BẢNG CHI TIẾT ĐƠN HÀNG (OrderDetails)
IF OBJECT_ID(N'dbo.OrderDetails', N'U') IS NULL
CREATE TABLE dbo.OrderDetails (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderDetails PRIMARY KEY,
    OrderId INT NOT NULL,
    ProductId INT NOT NULL,
    ProductName NVARCHAR(250) NOT NULL,
    ProductImageUrl NVARCHAR(500) NULL,
    Quantity INT NOT NULL,
    UnitPrice DECIMAL(18,0) NOT NULL,
    SubTotal DECIMAL(18,0) NOT NULL,
    CONSTRAINT FK_OrderDetails_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders (Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderDetails_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id)
);
GO

-- 12. BẢNG ĐÁNH GIÁ (Reviews)
IF OBJECT_ID(N'dbo.Reviews', N'U') IS NULL
CREATE TABLE dbo.Reviews (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Reviews PRIMARY KEY,
    ProductId INT NOT NULL,
    UserId NVARCHAR(450) NOT NULL,
    OrderId INT NOT NULL,
    Rating INT NOT NULL DEFAULT 5,
    Comment NVARCHAR(MAX) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsApproved BIT NOT NULL DEFAULT 1,
    AdminReply NVARCHAR(MAX) NULL,
    AdminReplyDate DATETIME2 NULL,
    CONSTRAINT FK_Reviews_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE,
    CONSTRAINT FK_Reviews_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id)
);
GO

-- 13. BẢNG DANH SÁCH YÊU THÍCH (WishlistItems)
IF OBJECT_ID(N'dbo.WishlistItems', N'U') IS NULL
CREATE TABLE dbo.WishlistItems (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_WishlistItems PRIMARY KEY,
    UserId NVARCHAR(450) NOT NULL,
    ProductId INT NOT NULL,
    AddedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT FK_WishlistItems_AspNetUsers FOREIGN KEY (UserId) REFERENCES dbo.AspNetUsers (Id) ON DELETE CASCADE,
    CONSTRAINT FK_WishlistItems_Products FOREIGN KEY (ProductId) REFERENCES dbo.Products (Id) ON DELETE CASCADE
);
GO

-- 14. BẢNG BANNER QUẢNG CÁO (Banners)
IF OBJECT_ID(N'dbo.Banners', N'U') IS NULL
CREATE TABLE dbo.Banners (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Banners PRIMARY KEY,
    Title NVARCHAR(200) NOT NULL,
    SubTitle NVARCHAR(300) NULL,
    ImageUrl NVARCHAR(500) NOT NULL,
    LinkUrl NVARCHAR(500) NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- 15. BẢNG BÀI VIẾT TIN TỨC (NewsPosts)
IF OBJECT_ID(N'dbo.NewsPosts', N'U') IS NULL
CREATE TABLE dbo.NewsPosts (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NewsPosts PRIMARY KEY,
    Title NVARCHAR(300) NOT NULL,
    Slug NVARCHAR(300) NOT NULL CONSTRAINT UQ_NewsPosts_Slug UNIQUE,
    Summary NVARCHAR(500) NULL,
    Content NVARCHAR(MAX) NOT NULL,
    ImageUrl NVARCHAR(500) NULL,
    Author NVARCHAR(100) NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    ViewCount INT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1
);
GO

-- 16. BẢNG LIÊN HỆ GÓP Ý (ContactMessages)
IF OBJECT_ID(N'dbo.ContactMessages', N'U') IS NULL
CREATE TABLE dbo.ContactMessages (
    Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_ContactMessages PRIMARY KEY,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    Phone NVARCHAR(50) NULL,
    Subject NVARCHAR(250) NULL,
    Message NVARCHAR(MAX) NOT NULL,
    CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),
    IsRead BIT NOT NULL DEFAULT 0
);
GO
