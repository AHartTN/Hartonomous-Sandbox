# Hartonomous Documentation Index

Welcome to the Hartonomous documentation hub. This index provides navigation to all documentation resources for understanding, deploying, and operating the Hartonomous multimodal AI platform.

## 📖 Core Documentation

### Getting Started

| Document | Description | Audience |
|----------|-------------|----------|
| **[Main README](../README.md)** | Project overview, quick start, and feature summary | Everyone |
| **[Architecture Overview](architecture.md)** | System design, components, data flow, and patterns | Architects, Developers |
| **[Deployment Guide](deployment.md)** | Complete deployment instructions for all environments | DevOps, Administrators |

### Development

| Document | Description | Audience |
|----------|-------------|----------|
| **[Development Guide](development.md)** | Local setup, testing, debugging, and contribution guide | Developers |
| **[API Reference](api-reference.md)** | Complete API documentation with usage examples | Developers |
| **[Data Model](data-model.md)** | Entity relationships, schema, and data architecture | Developers, DBAs |

### Operations

| Document | Description | Audience |
|----------|-------------|----------|
| **[Operations Guide](operations.md)** | Day-to-day operations, monitoring, and troubleshooting | Administrators, SREs |

### Quality Assurance

| Document | Description | Audience |
|----------|-------------|----------|
| **[Technical Audit Report](technical-audit-report.md)** | Comprehensive technology validation with external sources | Technical Leadership, Architects |
| **[Audit Executive Summary](audit-executive-summary.md)** | High-level audit findings and compliance summary | Executives, Management |

## 🔍 Reference Documentation

### Component Documentation

| Document | Description | Last Updated |
|----------|-------------|--------------|
| **[Component Inventory](component-inventory.md)** | Project catalog, cleanup history, and data asset status | 2025-11-01 |
| **[Workspace Survey](workspace-survey-20251101.md)** | Detailed repository analysis and recommendations | 2025-11-01 |

### Technical References

- **SQL Procedures**: See `../sql/procedures/` directory for stored procedure implementations
- **Neo4j Schema**: See `../neo4j/schemas/CoreSchema.cypher` for graph database schema
- **Entity Configurations**: See `../src/Hartonomous.Data/Configurations/` for EF Core entity configurations

## 🗺️ Documentation Roadmap

### By Role

**New Developers:**

1. Read [Main README](../README.md)
2. Follow [Development Guide](development.md)
3. Review [Architecture Overview](architecture.md)
4. Explore [API Reference](api-reference.md)

**System Administrators:**

1. Review [Deployment Guide](deployment.md)
2. Study [Operations Guide](operations.md)
3. Understand [Data Model](data-model.md)
4. Bookmark [Troubleshooting Section](operations.md#troubleshooting)

**Architects:**

1. Study [Architecture Overview](architecture.md)
2. Review [Data Model](data-model.md)
3. Examine [Workspace Survey](workspace-survey-20251101.md)
4. Analyze [Component Inventory](component-inventory.md)

**End Users:**

1. Read [Main README](../README.md)
2. Follow Quick Start in [Main README](../README.md#-quick-start)
3. Consult [Operations Guide](operations.md) for common tasks

## 📊 Document Status

| Document | Status | Completeness |
|----------|--------|--------------|
| Architecture Overview | ✅ Complete | 100% |
| Deployment Guide | ✅ Complete | 100% |
| API Reference | ✅ Complete | 100% |
| Data Model | ✅ Complete | 100% |
| Operations Guide | ✅ Complete | 100% |
| Development Guide | ✅ Complete | 100% |
| Technical Audit Report | ✅ Complete | 100% |
| Audit Executive Summary | ✅ Complete | 100% |
| Component Inventory | ✅ Complete | 100% |
| Workspace Survey | ✅ Complete | 100% |
| SQL Procedures Ref | 🔄 In Progress | 60% (inline documented) |

## 🔗 Quick Links

### External Resources

- [.NET 10 Documentation](https://docs.microsoft.com/dotnet/)
- [SQL Server 2025 Documentation](https://docs.microsoft.com/sql/)
- [Neo4j Documentation](https://neo4j.com/docs/)
- [Azure Event Hubs](https://docs.microsoft.com/azure/event-hubs/)

### Repository Links

- [GitHub Repository](https://github.com/AHartTN/Hartonomous)
- [Issue Tracker](https://github.com/AHartTN/Hartonomous/issues)
- [Pull Requests](https://github.com/AHartTN/Hartonomous/pulls)

## 📝 Documentation Standards

All documentation in this repository follows these standards:

- **Markdown Format**: All docs use GitHub-flavored Markdown
- **Code Examples**: Include working, tested code samples
- **Version Information**: Updated dates and version tags
- **Navigation**: Cross-links between related documents
- **Accessibility**: Clear headings, tables, and structured content

## 🤝 Contributing to Documentation

Found an error or want to improve the docs?

1. Create an issue describing the problem
2. Fork the repository
3. Make your changes following the documentation standards
4. Submit a pull request

See [Development Guide](development.md#contributing) for detailed contribution guidelines.

## 📧 Getting Help

- **Questions**: Create an issue with the `question` label
- **Bug Reports**: Use the bug report template
- **Feature Requests**: Use the feature request template
- **General Discussion**: Check existing discussions

---

**Last Updated**: November 1, 2025  
**Maintainer**: Hartonomous Development Team  
**License**: MIT
