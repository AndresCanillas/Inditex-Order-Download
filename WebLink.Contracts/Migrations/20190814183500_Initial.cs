using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

namespace WebLink.Contracts.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Materials",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: true),
                    Properties = table.Column<string>(nullable: true),
                    ShowAsMaterial = table.Column<bool>(nullable: false),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Materials", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "PrintedLabels",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    PrinterID = table.Column<int>(nullable: false),
                    CompanyID = table.Column<int>(nullable: false),
                    ProjectID = table.Column<int>(nullable: false),
                    ArticleCode = table.Column<string>(maxLength: 25, nullable: true),
                    ProductCode = table.Column<string>(maxLength: 25, nullable: true),
                    ProductionType = table.Column<int>(nullable: false),
                    ProductionLocationID = table.Column<int>(nullable: false),
                    Serial = table.Column<long>(nullable: false),
                    TID = table.Column<string>(maxLength: 32, nullable: true),
                    EPC = table.Column<string>(maxLength: 32, nullable: true),
                    AccessPassword = table.Column<string>(maxLength: 8, nullable: true),
                    KillPassword = table.Column<string>(maxLength: 8, nullable: true),
                    Success = table.Column<bool>(maxLength: 200, nullable: false),
                    ErrorCode = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrintedLabels", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "RFIDParameters",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    SerializedConfig = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RFIDParameters", x => x.ID);
                });

            migrationBuilder.CreateTable(
                name: "SerialSequences",
                columns: table => new
                {
                    ID = table.Column<string>(maxLength: 40, nullable: false),
                    Filter = table.Column<string>(maxLength: 100, nullable: false),
                    NextValue = table.Column<long>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerialSequences", x => new { x.ID, x.Filter });
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(maxLength: 50, nullable: true),
                    MainLocationID = table.Column<int>(nullable: true),
                    MainContact = table.Column<string>(maxLength: 50, nullable: true),
                    MainContactEmail = table.Column<string>(maxLength: 50, nullable: true),
                    Culture = table.Column<string>(maxLength: 10, nullable: true),
                    Instructions = table.Column<string>(maxLength: 2000, nullable: true),
                    Logo = table.Column<byte[]>(nullable: true),
                    CompanyCode = table.Column<string>(maxLength: 12, nullable: true),
                    IDTZone = table.Column<string>(maxLength: 30, nullable: true),
                    GSTCode = table.Column<string>(maxLength: 6, nullable: true),
                    GSTID = table.Column<int>(nullable: true),
                    SLADays = table.Column<int>(nullable: true),
                    DefaultProductionLocation = table.Column<int>(nullable: true),
                    ShowAsCompany = table.Column<bool>(nullable: false),
                    FtpUser = table.Column<string>(maxLength: 20, nullable: true),
                    FtpPassword = table.Column<string>(maxLength: 500, nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false),
                    RFIDConfigID = table.Column<int>(nullable: true),
                    OrderSort = table.Column<string>(maxLength: 2000, nullable: true),
                    HeaderFields = table.Column<string>(maxLength: 200, nullable: true),
                    StopFields = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Companies_RFIDParameters_RFIDConfigID",
                        column: x => x.RFIDConfigID,
                        principalTable: "RFIDParameters",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Brands",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CompanyID = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 50, nullable: true),
                    Icon = table.Column<byte[]>(nullable: true),
                    EnableFTPFolder = table.Column<bool>(nullable: false),
                    FTPFolder = table.Column<string>(nullable: true),
                    RFIDConfigID = table.Column<int>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Brands", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Brands_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Brands_RFIDParameters_RFIDConfigID",
                        column: x => x.RFIDConfigID,
                        principalTable: "RFIDParameters",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CompanyProviders",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CompanyID = table.Column<int>(nullable: false),
                    ProviderCompanyID = table.Column<int>(nullable: false),
                    Instructions = table.Column<string>(nullable: true),
                    SLADays = table.Column<int>(nullable: true),
                    DefaultProductionLocation = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProviders", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CompanyProviders_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Contacts",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CompanyID = table.Column<int>(nullable: false),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    MobileNumber = table.Column<string>(nullable: true),
                    Comments = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contacts", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Contacts_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Locations",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CompanyID = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 50, nullable: true),
                    DeliverTo = table.Column<string>(maxLength: 50, nullable: true),
                    AddressLine1 = table.Column<string>(maxLength: 50, nullable: true),
                    AddressLine2 = table.Column<string>(maxLength: 50, nullable: true),
                    CityOrTown = table.Column<string>(maxLength: 50, nullable: true),
                    StateOrProvince = table.Column<string>(maxLength: 50, nullable: true),
                    Country = table.Column<string>(maxLength: 30, nullable: true),
                    ZipCode = table.Column<string>(maxLength: 8, nullable: true),
                    LocationCode = table.Column<string>(maxLength: 25, nullable: true),
                    GSTCity = table.Column<string>(maxLength: 25, nullable: true),
                    GSTProvince = table.Column<string>(maxLength: 6, nullable: true),
                    GSTCountry = table.Column<int>(nullable: false),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Locations", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Locations_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CompanyID = table.Column<int>(nullable: false),
                    Type = table.Column<int>(nullable: false),
                    IntendedRole = table.Column<string>(maxLength: 30, nullable: true),
                    IntendedUser = table.Column<string>(maxLength: 30, nullable: true),
                    NKey = table.Column<string>(maxLength: 120, nullable: true),
                    Source = table.Column<string>(maxLength: 50, nullable: true),
                    Title = table.Column<string>(maxLength: 100, nullable: true),
                    Message = table.Column<string>(nullable: true),
                    Data = table.Column<string>(nullable: true),
                    AutoDismiss = table.Column<bool>(nullable: false),
                    Count = table.Column<int>(nullable: false),
                    Action = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Notifications_Companies_CompanyID",
                        column: x => x.CompanyID,
                        principalTable: "Companies",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    BrandID = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 35, nullable: true),
                    Description = table.Column<string>(maxLength: 4000, nullable: true),
                    ProjectCode = table.Column<string>(maxLength: 20, nullable: true),
                    EnableFTPFolder = table.Column<bool>(nullable: false),
                    FTPFolder = table.Column<string>(nullable: true),
                    RFIDConfigID = table.Column<int>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false),
                    Hidden = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Projects_Brands_BrandID",
                        column: x => x.BrandID,
                        principalTable: "Brands",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Projects_RFIDParameters_RFIDConfigID",
                        column: x => x.RFIDConfigID,
                        principalTable: "RFIDParameters",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Addresses",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ContactID = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 35, nullable: true),
                    AddressLine1 = table.Column<string>(maxLength: 40, nullable: true),
                    AddressLine2 = table.Column<string>(maxLength: 40, nullable: true),
                    CityOrTown = table.Column<string>(maxLength: 25, nullable: true),
                    StateOrProvince = table.Column<string>(maxLength: 25, nullable: true),
                    Country = table.Column<string>(maxLength: 25, nullable: true),
                    ZipCode = table.Column<string>(maxLength: 8, nullable: true),
                    CreatedBy = table.Column<string>(nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Addresses", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Addresses_Contacts_ContactID",
                        column: x => x.ContactID,
                        principalTable: "Contacts",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Printers",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DeviceID = table.Column<string>(maxLength: 20, nullable: true),
                    ProductName = table.Column<string>(maxLength: 50, nullable: true),
                    Name = table.Column<string>(maxLength: 35, nullable: true),
                    FirmwareVersion = table.Column<string>(maxLength: 30, nullable: true),
                    LastSeenOnline = table.Column<DateTime>(nullable: true),
                    LocationID = table.Column<int>(nullable: false),
                    DriverName = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Printers", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Printers_Locations_LocationID",
                        column: x => x.LocationID,
                        principalTable: "Locations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Catalogs",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProjectID = table.Column<int>(nullable: false),
                    CatalogID = table.Column<int>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    Captions = table.Column<string>(nullable: true),
                    Definition = table.Column<string>(nullable: true),
                    SortOrder = table.Column<int>(nullable: false),
                    IsSystem = table.Column<bool>(nullable: false),
                    IsHidden = table.Column<bool>(nullable: false),
                    IsReadonly = table.Column<bool>(nullable: false),
                    RequiredRoles = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Catalogs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Catalogs_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanyOrders",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProjectID = table.Column<int>(nullable: false),
                    OrderDataID = table.Column<int>(nullable: false),
                    OrderNumber = table.Column<string>(maxLength: 16, nullable: true),
                    OrderDate = table.Column<DateTime>(nullable: false),
                    UserName = table.Column<string>(maxLength: 50, nullable: true),
                    Source = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    ProductionType = table.Column<int>(nullable: false),
                    AssignedPrinterID = table.Column<int>(nullable: true),
                    OrderStatus = table.Column<int>(nullable: false),
                    ConfirmedByMD = table.Column<bool>(nullable: false),
                    LocationID = table.Column<int>(nullable: true),
                    PreviewGenerated = table.Column<bool>(nullable: false),
                    BillTo = table.Column<string>(maxLength: 30, nullable: true),
                    SendTo = table.Column<string>(maxLength: 30, nullable: true),
                    BillToCompanyID = table.Column<int>(nullable: false),
                    SendToCompanyID = table.Column<int>(nullable: false),
                    SendToLocationID = table.Column<int>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyOrders", x => x.ID);
                    table.ForeignKey(
                        name: "FK_CompanyOrders_Locations_LocationID",
                        column: x => x.LocationID,
                        principalTable: "Locations",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CompanyOrders_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataImportMappings",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProjectID = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 100, nullable: true),
                    RootCatalog = table.Column<int>(nullable: false),
                    SourceType = table.Column<string>(maxLength: 20, nullable: true),
                    FileNameMask = table.Column<string>(maxLength: 50, nullable: true),
                    SourceCulture = table.Column<string>(maxLength: 10, nullable: true),
                    Encoding = table.Column<string>(maxLength: 20, nullable: true),
                    LineDelimiter = table.Column<string>(maxLength: 5, nullable: true),
                    ColumnDelimiter = table.Column<string>(maxLength: 1, nullable: true),
                    QuotationChar = table.Column<string>(maxLength: 1, nullable: true),
                    IncludeHeader = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataImportMappings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DataImportMappings_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Labels",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProjectID = table.Column<int>(nullable: true),
                    Name = table.Column<string>(maxLength: 50, nullable: true),
                    Comments = table.Column<string>(nullable: true),
                    EncodeRFID = table.Column<bool>(nullable: false),
                    PreviewData = table.Column<string>(nullable: true),
                    Type = table.Column<int>(nullable: false),
                    MaterialID = table.Column<int>(nullable: true),
                    Mappings = table.Column<string>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Labels", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Labels_Materials_MaterialID",
                        column: x => x.MaterialID,
                        principalTable: "Materials",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Labels_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Packs",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProjectID = table.Column<int>(nullable: false),
                    Name = table.Column<string>(maxLength: 30, nullable: true),
                    Description = table.Column<string>(nullable: true),
                    PackCode = table.Column<string>(maxLength: 25, nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Packs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Packs_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrinterJobs",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    CompanyID = table.Column<int>(nullable: false),
                    CompanyOrderID = table.Column<int>(nullable: false),
                    ProjectID = table.Column<int>(nullable: false),
                    ProductionLocationID = table.Column<int>(nullable: true),
                    AssignedPrinter = table.Column<int>(nullable: true),
                    ArticleID = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    Printed = table.Column<int>(nullable: false),
                    Errors = table.Column<int>(nullable: false),
                    Extras = table.Column<int>(nullable: false),
                    DueDate = table.Column<DateTime>(nullable: true),
                    Status = table.Column<int>(nullable: false),
                    AutoStart = table.Column<bool>(nullable: false),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    CompletedDate = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterJobs", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrinterJobs_CompanyOrders_CompanyOrderID",
                        column: x => x.CompanyOrderID,
                        principalTable: "CompanyOrders",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DataImportColMapping",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    DataImportMappingID = table.Column<int>(nullable: true),
                    ColOrder = table.Column<int>(nullable: false),
                    IsFixedValue = table.Column<bool>(nullable: true),
                    FixedValue = table.Column<string>(nullable: true),
                    FixedType = table.Column<int>(nullable: true),
                    InputColumn = table.Column<string>(nullable: true),
                    Ignore = table.Column<bool>(nullable: true),
                    Type = table.Column<int>(nullable: true),
                    MaxLength = table.Column<int>(nullable: true),
                    MinLength = table.Column<int>(nullable: true),
                    MinValue = table.Column<long>(nullable: true),
                    MaxValue = table.Column<long>(nullable: true),
                    MinDate = table.Column<DateTime>(nullable: true),
                    MaxDate = table.Column<DateTime>(nullable: true),
                    DecimalPlaces = table.Column<int>(nullable: true),
                    Function = table.Column<int>(nullable: true),
                    FunctionArguments = table.Column<string>(nullable: true),
                    CanBeEmpty = table.Column<bool>(nullable: true),
                    TargetColumn = table.Column<string>(nullable: true),
                    IsPK = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataImportColMapping", x => x.ID);
                    table.ForeignKey(
                        name: "FK_DataImportColMapping_DataImportMappings_DataImportMappingID",
                        column: x => x.DataImportMappingID,
                        principalTable: "DataImportMappings",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    ProjectID = table.Column<int>(nullable: true),
                    Name = table.Column<string>(maxLength: 30, nullable: true),
                    Description = table.Column<string>(maxLength: 2000, nullable: true),
                    ArticleCode = table.Column<string>(maxLength: 25, nullable: true),
                    BillingCode = table.Column<string>(maxLength: 25, nullable: true),
                    LabelID = table.Column<int>(nullable: true),
                    CreatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(nullable: false),
                    UpdatedBy = table.Column<string>(maxLength: 50, nullable: true),
                    UpdatedDate = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.ID);
                    table.ForeignKey(
                        name: "FK_Articles_Labels_LabelID",
                        column: x => x.LabelID,
                        principalTable: "Labels",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Articles_Projects_ProjectID",
                        column: x => x.ProjectID,
                        principalTable: "Projects",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PrinterJobDetails",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    PrinterJobID = table.Column<int>(nullable: false),
                    ProductDataID = table.Column<int>(nullable: false),
                    Quantity = table.Column<int>(nullable: false),
                    Printed = table.Column<int>(nullable: false),
                    Errors = table.Column<int>(nullable: false),
                    Extras = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterJobDetails", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrinterJobDetails_PrinterJobs_PrinterJobID",
                        column: x => x.PrinterJobID,
                        principalTable: "PrinterJobs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PackArticles",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    PackID = table.Column<int>(nullable: false),
                    ArticleID = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PackArticles", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PackArticles_Articles_ArticleID",
                        column: x => x.ArticleID,
                        principalTable: "Articles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PackArticles_Packs_PackID",
                        column: x => x.PackID,
                        principalTable: "Packs",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrinterSettings",
                columns: table => new
                {
                    ID = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn),
                    PrinterID = table.Column<int>(nullable: false),
                    ArticleID = table.Column<int>(nullable: false),
                    XOffset = table.Column<double>(nullable: false),
                    YOffset = table.Column<double>(nullable: false),
                    Speed = table.Column<int>(nullable: false),
                    Darkness = table.Column<int>(nullable: false),
                    Rotated = table.Column<bool>(nullable: false),
                    ChangeOrientation = table.Column<bool>(nullable: false),
                    PauseOnError = table.Column<bool>(nullable: false),
                    EnableCut = table.Column<bool>(nullable: false),
                    CutBehavior = table.Column<int>(nullable: false),
                    ResumeAfterCut = table.Column<bool>(nullable: false),
                    PrintHeaders = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrinterSettings", x => x.ID);
                    table.ForeignKey(
                        name: "FK_PrinterSettings_Articles_ArticleID",
                        column: x => x.ArticleID,
                        principalTable: "Articles",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrinterSettings_Printers_PrinterID",
                        column: x => x.PrinterID,
                        principalTable: "Printers",
                        principalColumn: "ID",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Addresses_ContactID",
                table: "Addresses",
                column: "ContactID");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_LabelID",
                table: "Articles",
                column: "LabelID");

            migrationBuilder.CreateIndex(
                name: "IX_Articles_ProjectID",
                table: "Articles",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_CompanyID",
                table: "Brands",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_Brands_RFIDConfigID",
                table: "Brands",
                column: "RFIDConfigID");

            migrationBuilder.CreateIndex(
                name: "IX_Catalogs_ProjectID",
                table: "Catalogs",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_RFIDConfigID",
                table: "Companies",
                column: "RFIDConfigID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_LocationID",
                table: "CompanyOrders",
                column: "LocationID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyOrders_ProjectID",
                table: "CompanyOrders",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyProviders_CompanyID",
                table: "CompanyProviders",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_Contacts_CompanyID",
                table: "Contacts",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_DataImportColMapping_DataImportMappingID",
                table: "DataImportColMapping",
                column: "DataImportMappingID");

            migrationBuilder.CreateIndex(
                name: "IX_DataImportMappings_ProjectID",
                table: "DataImportMappings",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Labels_MaterialID",
                table: "Labels",
                column: "MaterialID");

            migrationBuilder.CreateIndex(
                name: "IX_Labels_ProjectID",
                table: "Labels",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_CompanyID",
                table: "Locations",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_CompanyID",
                table: "Notifications",
                column: "CompanyID");

            migrationBuilder.CreateIndex(
                name: "IX_PackArticles_ArticleID",
                table: "PackArticles",
                column: "ArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_PackArticles_PackID",
                table: "PackArticles",
                column: "PackID");

            migrationBuilder.CreateIndex(
                name: "IX_Packs_ProjectID",
                table: "Packs",
                column: "ProjectID");

            migrationBuilder.CreateIndex(
                name: "IX_PrinterJobDetails_PrinterJobID",
                table: "PrinterJobDetails",
                column: "PrinterJobID");

            migrationBuilder.CreateIndex(
                name: "IX_PrinterJobs_CompanyOrderID",
                table: "PrinterJobs",
                column: "CompanyOrderID");

            migrationBuilder.CreateIndex(
                name: "IX_Printers_LocationID",
                table: "Printers",
                column: "LocationID");

            migrationBuilder.CreateIndex(
                name: "IX_PrinterSettings_ArticleID",
                table: "PrinterSettings",
                column: "ArticleID");

            migrationBuilder.CreateIndex(
                name: "IX_PrinterSettings_PrinterID",
                table: "PrinterSettings",
                column: "PrinterID");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_BrandID",
                table: "Projects",
                column: "BrandID");

            migrationBuilder.CreateIndex(
                name: "IX_Projects_RFIDConfigID",
                table: "Projects",
                column: "RFIDConfigID");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Addresses");

            migrationBuilder.DropTable(
                name: "Catalogs");

            migrationBuilder.DropTable(
                name: "CompanyProviders");

            migrationBuilder.DropTable(
                name: "DataImportColMapping");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PackArticles");

            migrationBuilder.DropTable(
                name: "PrintedLabels");

            migrationBuilder.DropTable(
                name: "PrinterJobDetails");

            migrationBuilder.DropTable(
                name: "PrinterSettings");

            migrationBuilder.DropTable(
                name: "SerialSequences");

            migrationBuilder.DropTable(
                name: "Contacts");

            migrationBuilder.DropTable(
                name: "DataImportMappings");

            migrationBuilder.DropTable(
                name: "Packs");

            migrationBuilder.DropTable(
                name: "PrinterJobs");

            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "Printers");

            migrationBuilder.DropTable(
                name: "CompanyOrders");

            migrationBuilder.DropTable(
                name: "Labels");

            migrationBuilder.DropTable(
                name: "Locations");

            migrationBuilder.DropTable(
                name: "Materials");

            migrationBuilder.DropTable(
                name: "Projects");

            migrationBuilder.DropTable(
                name: "Brands");

            migrationBuilder.DropTable(
                name: "Companies");

            migrationBuilder.DropTable(
                name: "RFIDParameters");
        }
    }
}
