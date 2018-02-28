//---------------------------------------------------------------------
// <copyright file="CsdlReaderTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using FluentAssertions;
using Microsoft.OData.Edm.Csdl;
using Microsoft.OData.Edm.Csdl.CsdlSemantics;
using Microsoft.OData.Edm.Validation;
using Microsoft.OData.Edm.Vocabularies;
using Microsoft.OData.Edm.Vocabularies.V1;
using Xunit;
using ErrorStrings = Microsoft.OData.Edm.Strings;

namespace Microsoft.OData.Edm.Tests.Csdl
{
    public class CsdlReaderTests
    {
        private const string ValidEdmx = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Test"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityContainer Name=""Container"" />
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
        private const string InvalidXml = "<fake/>";
        private const string ErrorMessage = "UnexpectedXmlElement : The element 'fake' was unexpected for the root element. The root element should be Edmx. : (0, 0)";

        private XmlReader validReader;
        private XmlReader invalidReader;

        public CsdlReaderTests()
        {
            this.validReader = XElement.Parse(ValidEdmx).CreateReader();
            this.invalidReader = XElement.Parse(InvalidXml).CreateReader();
        }

        [Fact]
        public void ReadNavigationPropertyPartnerTest()
        {
            var csdl =
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                    "<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\">" +
                        "<edmx:DataServices>" +
                            "<Schema Namespace=\"NS\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\">" +
                                "<EntityType Name=\"EntityType1\">" +
                                    "<Key><PropertyRef Name=\"ID\" /></Key>" +
                                    "<Property Name=\"ID\" Type=\"Edm.Int32\" />" +
                                    "<Property Name=\"ComplexProp\" Type=\"Collection(NS.ComplexType1)\" Nullable=\"false\" />" +
                                    "<NavigationProperty Name=\"OuterNavA\" Type=\"NS.EntityType2\" Nullable=\"false\" Partner=\"OuterNavA\" />" +
                                    "<NavigationProperty Name=\"OuterNavB\" Type=\"Collection(NS.EntityType2)\" Partner=\"NS.EntityType3/OuterNavC\" />" +
                                "</EntityType>" +
                                "<EntityType Name=\"EntityType2\">" +
                                    "<Key><PropertyRef Name=\"ID\" /></Key>" +
                                    "<Property Name=\"ID\" Type=\"Edm.Int32\" />" +
                                    "<Property Name=\"ComplexProp\" Type=\"NS.ComplexType2\" Nullable=\"false\" />" +
                                    "<NavigationProperty Name=\"OuterNavA\" Type=\"NS.EntityType1\" Nullable=\"false\" Partner=\"OuterNavA\" />" +
                                    "<NavigationProperty Name=\"OuterNavB\" Type=\"NS.EntityType1\" Nullable=\"false\" Partner=\"ComplexProp/InnerNav\" />" +
                                "</EntityType>" +
                                "<EntityType Name=\"EntityType3\" BaseType=\"NS.EntityType2\">" +
                                    "<NavigationProperty Name=\"OuterNavC\" Type=\"Collection(NS.EntityType1)\" Partner=\"OuterNavB\" />" +
                                "</EntityType>" +
                                "<ComplexType Name=\"ComplexType1\">" +
                                    "<NavigationProperty Name=\"InnerNav\" Type=\"NS.EntityType2\" Nullable=\"false\" />" +
                                "</ComplexType>" +
                                "<ComplexType Name=\"ComplexType2\">" +
                                    "<NavigationProperty Name=\"InnerNav\" Type=\"NS.EntityType1\" Nullable=\"false\" />" +
                                "</ComplexType>" +
                            "</Schema>" +
                        "</edmx:DataServices>" +
                    "</edmx:Edmx>";
            var model = CsdlReader.Parse(XElement.Parse(csdl).CreateReader());
            var entityType1 = (IEdmEntityType)model.FindDeclaredType("NS.EntityType1");
            var entityType2 = (IEdmEntityType)model.FindDeclaredType("NS.EntityType2");
            var entityType3 = (IEdmEntityType)model.FindDeclaredType("NS.EntityType3");
            var complexType1 = (IEdmComplexType)model.FindDeclaredType("NS.ComplexType1");
            var complexType2 = (IEdmComplexType)model.FindDeclaredType("NS.ComplexType2");
            Assert.Equal("OuterNavA", ((IEdmNavigationProperty)entityType1.FindProperty("OuterNavA")).GetPartnerPath().Path);
            Assert.Equal(
                entityType2.FindProperty("OuterNavA"),
                ((IEdmNavigationProperty)entityType1.FindProperty("OuterNavA")).Partner);
            Assert.Equal("ComplexProp/InnerNav", ((IEdmNavigationProperty)entityType2.FindProperty("OuterNavB")).GetPartnerPath().Path);
            Assert.Equal(
                complexType1.FindProperty("InnerNav"),
                ((IEdmNavigationProperty)entityType2.FindProperty("OuterNavB")).Partner);
            Assert.Equal("NS.EntityType3/OuterNavC", ((IEdmNavigationProperty)entityType1.FindProperty("OuterNavB")).GetPartnerPath().Path);
            Assert.Equal(
                entityType3.FindProperty("OuterNavC"),
                ((IEdmNavigationProperty)entityType1.FindProperty("OuterNavB")).Partner);
        }

        [Fact]
        public void ValidateNavigationPropertyBindingPathTypeCast()
        {
            var csdl
                = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                    "<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\">" +
                      "<edmx:DataServices>" +
                        "<Schema Namespace=\"NS\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\">" +
                          "<EntityType Name=\"EntityA\">" +
                            "<Key><PropertyRef Name=\"ID\" /></Key>" +
                              "<Property Name=\"ID\" Type=\"Edm.Int32\" Nullable=\"false\" />" +
                              "<Property Name=\"Complex\" Type=\"NS.ComplexA\" Nullable=\"false\" />" +
                            "</EntityType>" +
                          "<EntityType Name=\"EntityB\">" +
                            "<Key><PropertyRef Name=\"ID\" /></Key>" +
                            "<Property Name=\"ID\" Type=\"Edm.Int32\" Nullable=\"false\" />" +
                          "</EntityType>" +
                          "<ComplexType Name=\"ComplexA\" />" +
                          "<ComplexType Name=\"ComplexB\">" +
                            "<NavigationProperty Name=\"ComplexBNav\" Type=\"NS.EntityB\" Nullable=\"false\" />" +
                          "</ComplexType>" +
                          "<EntityContainer Name=\"Container\">" +
                            "<EntitySet Name=\"Set1\" EntityType=\"NS.EntityA\">" +
                              "<NavigationPropertyBinding Path=\"Complex/NS.ComplexB/ComplexBNav\" Target=\"Set2\" />" +
                            "</EntitySet>" +
                            "<EntitySet Name=\"Set2\" EntityType=\"NS.EntityB\" />" +
                          "</EntityContainer>" +
                        "</Schema>" +
                      "</edmx:DataServices>" +
                    "</edmx:Edmx>";
            var model = CsdlReader.Parse(XElement.Parse(csdl).CreateReader());
            var set1 = model.FindDeclaredNavigationSource("Set1");
            Assert.True(set1.NavigationPropertyBindings.First().NavigationProperty is UnresolvedNavigationPropertyPath);
        }

        [Fact]
        public void ValidateNavigationPropertyBindingPathTraversesNoNonContainmentNavigationProperties()
        {
            var csdl
                = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                    "<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\">" +
                      "<edmx:DataServices>" +
                        "<Schema Namespace=\"NS\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\">" +
                          "<EntityType Name=\"EntityBase\">" +
                            "<Key><PropertyRef Name=\"ID\" /></Key>" +
                            "<Property Name=\"ID\" Type=\"Edm.Int32\" Nullable=\"false\" />" +
                          "</EntityType>" +
                          "<EntityType Name=\"EntityA\" BaseType=\"NS.EntityBase\">" +
                            "<NavigationProperty Name=\"EntityAToB\" Type=\"NS.EntityB\" Nullable=\"false\" />" +
                          "</EntityType>" +
                          "<EntityType Name=\"EntityB\" BaseType=\"NS.EntityBase\">" +
                            "<NavigationProperty Name=\"EntityBToC\" Type=\"NS.EntityC\" Nullable=\"false\" />" +
                          "</EntityType>" +
                          "<EntityType Name=\"EntityC\" BaseType=\"NS.EntityBase\" />" +
                          "<EntityContainer Name=\"Container\">" +
                            "<EntitySet Name=\"SetA\" EntityType=\"NS.EntityA\">" +
                              "<NavigationPropertyBinding Path=\"EntityAToB/EntityBToC\" Target=\"SetC\" />" +
                            "</EntitySet>" +
                            "<EntitySet Name=\"SetB\" EntityType=\"NS.EntityB\" />" +
                            "<EntitySet Name=\"SetC\" EntityType=\"NS.EntityC\" />" +
                          "</EntityContainer>" +
                        "</Schema>" +
                      "</edmx:DataServices>" +
                    "</edmx:Edmx>";
            var model = CsdlReader.Parse(XElement.Parse(csdl).CreateReader());
            var setA = model.FindDeclaredNavigationSource("SetA");
            Assert.True(setA.NavigationPropertyBindings.First().NavigationProperty is UnresolvedNavigationPropertyPath);
        }

        [Fact]
        public void ValidateNavigationPropertyBindingPathTraversesContainmentNavigationProperties()
        {
            var csdl
                = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                    "<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\">" +
                      "<edmx:DataServices>" +
                        "<Schema Namespace=\"NS\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\">" +
                          "<EntityType Name=\"RootEntity\">" +
                            "<NavigationProperty Name=\"SetA\" Type=\"Collection(NS.EntityA)\" ContainsTarget=\"true\" />" +
                            "<NavigationProperty Name=\"SetB\" Type=\"Collection(NS.EntityB)\" ContainsTarget=\"true\" />" +
                          "</EntityType>" +
                          "<EntityType Name=\"EntityA\">" +
                            "<NavigationProperty Name=\"EntityAToB\" Type=\"Collection(NS.EntityB)\" />" +
                          "</EntityType>" +
                          "<EntityType Name=\"EntityB\"/>" +
                          "<EntityContainer Name=\"Container\">" +
                            "<Singleton Name=\"Root\" Type=\"NS.RootEntity\">" +
                              "<NavigationPropertyBinding Path=\"EntityA/EntityAToB\" Target=\"Root/SetB\" />" +
                            "</Singleton>" +
                          "</EntityContainer>" +
                        "</Schema>" +
                      "</edmx:DataServices>" +
                    "</edmx:Edmx>";
            var model = CsdlReader.Parse(XElement.Parse(csdl).CreateReader());
            //var setA = model.FindDeclaredNavigationSource("SetA");
            //Assert.True(setA.NavigationPropertyBindings.First().NavigationProperty is UnresolvedNavigationPropertyPath);
        }

        [Fact]
        public void ValidateEducationModel()
        {
            var csdl
                = "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                  "<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\">" +
                  "  <edmx:DataServices>" +
                  "    <Schema Namespace=\"Microsoft.Education.Rostering.Api\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\" xmlns:ags=\"http://aggregator.microsoft.com/internal\">" +
                  "      <!-- External references from the main Graph -->" +
                  "      <EnumType Name=\"physicalAddressType\">" +
                  "        <Member Name=\"unknown\" Value=\"0\" />" +
                  "        <Member Name=\"home\" Value=\"1\" />" +
                  "        <Member Name=\"business\" Value=\"2\" />" +
                  "        <Member Name=\"other\" Value=\"3\" />" +
                  "      </EnumType>" +
                  "      <ComplexType Name=\"physicalAddress\">" +
                  "        <Property Name=\"type\" Type=\"Microsoft.Education.Rostering.Api.physicalAddressType\" />" +
                  "        <Property Name=\"postOfficeBox\" Type=\"Edm.String\" Unicode=\"false\"/>" +
                  "        <Property Name=\"street\" Type=\"Edm.String\" Unicode=\"false\" />" +
                  "        <Property Name=\"city\" Type=\"Edm.String\" Unicode=\"false\" />" +
                  "        <Property Name=\"state\" Type=\"Edm.String\" Unicode=\"false\" />" +
                  "        <Property Name=\"countryOrRegion\" Type=\"Edm.String\" Unicode=\"false\" />" +
                  "        <Property Name=\"postalCode\" Type=\"Edm.String\" Unicode=\"false\" />" +
                  "      </ComplexType>" +
                  "      <ComplexType Name=\"identity\">" +
                  "        <Property Name=\"displayName\" Type=\"Edm.String\" />" +
                  "        <Property Name=\"id\" Type=\"Edm.String\" />" +
                  "      </ComplexType>" +
                  "      <ComplexType Name=\"identitySet\" OpenType=\"true\">" +
                  "        <Property Name=\"application\" Type=\"Microsoft.Education.Rostering.Api.identity\" ags:IsTransparentContainer=\"true\" />" +
                  "        <Property Name=\"device\" Type=\"Microsoft.Education.Rostering.Api.identity\" />" +
                  "        <Property Name=\"user\" Type=\"Microsoft.Education.Rostering.Api.identity\" ags:IsTransparentContainer=\"true\" />" +
                  "      </ComplexType>" +
                  "      <EntityType Name=\"directoryObject\" />" +
                  "      <EntityType Name=\"user\" />" +
                  "      <EntityType Name=\"group\" />" +
                  "      <EntityType Name=\"administrativeUnit\" />" +
                  "      <!-- New types for Rostering -->" +
                  "      <EnumType Name=\"educationRole\">" +
                  "        <Member Name=\"student\" Value=\"0\" />" +
                  "        <Member Name=\"teacher\" Value=\"1\" />" +
                  "        <Member Name=\"enum_sentinel\" Value=\"2\" />" +
                  "      </EnumType>" +
                  "      <EnumType Name=\"educationExternalSource\">" +
                  "        <Member Name=\"sis\" Value=\"0\" ags:WorkloadAlternateNames=\"us:system\"/>" +
                  "        <Member Name=\"sis\" Value=\"0\" ags:WorkloadAlternateNames=\"fr:systeme\"/>" +
                  "        <Member Name=\"manual\" Value=\"1\" ags:WorkloadAlternateNames=\"us:typed\"/>" +
                  "        <Member Name=\"enum_sentinel\" Value=\"2\" ags:NormalizedUnknownMember=\"true\" />" +
                  "      </EnumType>" +
                  "      <EnumType Name=\"educationGender\">" +
                  "        <Member Name=\"female\" Value=\"0\" ags:WorkloadAlternateNames=\"us:f,fem,girl,g\"/>" +
                  "        <Member Name=\"female\" Value=\"0\" ags:WorkloadAlternateNames=\"fr:f,fem,fille,f\"/>" +
                  "        <Member Name=\"male\" Value=\"1\" ags:WorkloadAlternateNames=\"us:m,mal,boy,b\"/>" +
                  "        <Member Name=\"male\" Value=\"1\" ags:WorkloadAlternateNames=\"fr:m,mal,garcon,gar,g\"/>" +
                  "        <Member Name=\"identifiesFemale\" Value=\"2\" ags:WorkloadAlternateNames=\"us:tf,transf,transg,tg\"/>" +
                  "        <Member Name=\"identifiesMale\" Value=\"3\" ags:WorkloadAlternateNames=\"us:tm,transb,transm,tb\"/>" +
                  "        <Member Name=\"intersex\" Value=\"4\" ags:WorkloadAlternateNames=\"us:is,both\"/>" +
                  "        <Member Name=\"other\" Value=\"5\" ags:WorkloadAlternateNames=\"us:ns,n/s\"/>" +
                  "        <Member Name=\"enum_sentinel\" Value=\"6\" />" +
                  "      </EnumType>" +
                  "      <EntityType Name=\"educationRoot\" ags:IsMaster=\"true\" ags:AddressUrl=\"https://fake_node\" >" +
                  "        <NavigationProperty Name=\"classes\" Type=\"Collection(Microsoft.Education.Rostering.Api.educationClass)\" ContainsTarget=\"true\" ags:WorkloadName=\"groups\" ags:AddressContainsEntitySetSegment=\"True\" ags:AddressUrl=\"https://graph.windows.net/v2/[tenantId]/groups/[sourceEntityId:0]\"  ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <NavigationProperty Name=\"schools\" Type=\"Collection(Microsoft.Education.Rostering.Api.educationSchool)\" ContainsTarget=\"true\" ags:WorkloadName=\"adminstrativeUnits\" ags:AddressContainsEntitySetSegment=\"True\" ags:AddressUrl=\"https://graph.windows.net/v2/[tenantId]/administrativeUnits/[sourceEntityId:0]\"  />" +
                  "        <NavigationProperty Name=\"users\" Type=\"Collection(Microsoft.Education.Rostering.Api.educationUser)\" ContainsTarget=\"true\" ags:WorkloadName=\"users\" ags:AddressContainsEntitySetSegment=\"True\" ags:AddressUrl=\"https://graph.windows.net/v2/[tenantId]/users/[sourceEntityId:0]\"  ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <NavigationProperty Name=\"me\" Type=\"Microsoft.Education.Rostering.Api.educationUser\" ContainsTarget=\"true\" ags:WorkloadName=\"me\" ags:AddressContainsEntitySetSegment=\"True\" ags:AddressUrl=\"https://graph.windows.net/v2/[tenantId]/users/[availableUserId]\"/>" +
                  "      </EntityType>" +
                  "      <EntityType Name=\"educationUser\" ags:IsMaster=\"true\" ags:AddressUrl=\"https://graph.windows.net/v2/\" BaseType=\"Microsoft.Education.Rostering.Api.user\" ags:FacadedServiceNamespace=\"Microsoft.DirectoryServices\" ags:WorkloadName=\"User\" ags:WorkloadQueryFilter=\"extension_fe2174665583431c953114ff7268b7b3_Education_ObjectType eq 'Student' or extension_fe2174665583431c953114ff7268b7b3_Education_ObjectType eq 'Teacher'\" ags:IncludePropertiesOnCreate='{\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByAppId\": \"@appid@\", \"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByUserId\": \"@userid@\"}' >" +
                  "        <Property Name=\"primaryRole\" Nullable=\"false\" Type=\"Microsoft.Education.Rostering.Api.educationRole\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_ObjectType\" />" +
                  "        <Property Name=\"middleName\" Type=\"Edm.String\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_MiddleName\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"stateId\" Type=\"Edm.String\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_StateId\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"externalSource\" Type=\"Microsoft.Education.Rostering.Api.educationExternalSource\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SyncSource\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"residenceAddress\" Nullable=\"true\" Type=\"Microsoft.Education.Rostering.Api.physicalAddress\" ags:IsTransparentContainer=\"true\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"residenceAddress.street\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_ResidenceAddress\" />" +
                  "        <Property Name=\"residenceAddress.city\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_ResidenceCity\" />" +
                  "        <Property Name=\"residenceAddress.state\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_ResidenceState\" />" +
                  "        <Property Name=\"residenceAddress.countryOrRegion\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_ResidenceCountry\" />" +
                  "        <Property Name=\"residenceAddress.postalCode\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_ResidenceZip\" />" +
                  "        <Property Name=\"mailingAddress\" Nullable=\"true\" Type=\"Microsoft.Education.Rostering.Api.physicalAddress\" ags:IsTransparentContainer=\"true\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"mailingAddress.street\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_MailingAddress\" />" +
                  "        <Property Name=\"mailingAddress.city\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_MailingCity\" />" +
                  "        <Property Name=\"mailingAddress.state\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_MailingState\" />" +
                  "        <Property Name=\"mailingAddress.countryOrRegion\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_MailingCountry\" />" +
                  "        <Property Name=\"mailingAddress.postalCode\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_MailingZip\" />" +
                  "        <Property Name=\"student\" Nullable=\"true\" Type=\"Microsoft.Education.Rostering.Api.educationStudent\" ags:IsTransparentContainer=\"true\" />" +
                  "        <Property Name=\"teacher\" Nullable=\"true\" Type=\"Microsoft.Education.Rostering.Api.educationTeacher\" ags:IsTransparentContainer=\"true\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"createdBy\" Nullable=\"true\" Type=\"Microsoft.Education.Rostering.Api.identitySet\" ags:IsTransparentContainer=\"true\" />" +
                  "        <Property Name=\"createdBy.application.displayName\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByAppDisplayName\" />" +
                  "        <Property Name=\"createdBy.application.id\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByAppId\" />" +
                  "        <Property Name=\"createdBy.user.displayName\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByUserDisplayName\" />" +
                  "        <Property Name=\"createdBy.user.id\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByUserId\" />" +
                  "        <!-- Properties to override the base class' AuthZ values - types do not have to be realistic - names are all that matters -->" +
                  "        <Property Name =\"mobilePhone\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"accountEnabled\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"givenName\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"assignedLicenses\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"assignedPlans\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"provisionedPlans\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"businessPhones\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"city\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"companyName\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"country\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"department\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"jobTitle\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"onPremisesImmutableId\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"onPremisesLastSyncDateTime\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"onPremisesSecurityIdentifier\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"onPremisesSyncEnabled\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"passwordPolicies\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"passwordProfile\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"officeLocation\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"postalCode\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"preferredLanguage\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"proxyAddresses\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"refreshTokensValidFromDateTime\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"showInAddressList\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"state\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"streetAddress\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"surname\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"usageLocation\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"userPrincipalName\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <Property Name =\"userType\" Type=\"Edm.String\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\" />" +
                  "        <!-- End override base class' AuthZ values -->" +
                  "        <NavigationProperty Name=\"schools\" Type=\"Collection(Microsoft.Education.Rostering.Api.educationSchool)\" ContainsTarget=\"false\" ags:WorkloadName=\"memberOf\" ags:WorkloadQueryCast=\"true\" ags:WorkloadClientFilter=\"true\" />" +
                  "        <NavigationProperty Name=\"classes\" Type=\"Collection(Microsoft.Education.Rostering.Api.educationClass)\" ContainsTarget=\"false\" ags:WorkloadName=\"memberOf\" ags:WorkloadQueryCast=\"true\" ags:WorkloadClientFilter=\"true\" />" +
                  "      </EntityType>" +
                  "      <ComplexType Name=\"educationStudent\" ags:IsMaster=\"true\">" +
                  "        <Property Name=\"graduationYear\" Type=\"Edm.String\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_GraduationYear\" />" +
                  "        <Property Name=\"grade\" Type=\"Edm.String\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_Grade\" />" +
                  "        <Property Name=\"birthDate\" Type=\"Edm.Date\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_DateOfBirth\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"gender\" Type=\"Microsoft.Education.Rostering.Api.educationGender\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_Gender\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"englishLanguageLearnersStatus\" Type=\"Edm.String\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_EnglishLanguageLearnersStatus\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"federalRace\" Type=\"Edm.String\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_FederalRace\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"studentNumber\" Type=\"Edm.String\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_StudentNumber\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"externalId\" Type=\"Edm.String\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SyncSource_StudentId\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "      </ComplexType>" +
                  "      <ComplexType Name=\"educationTeacher\" ags:IsMaster=\"true\">" +
                  "        <Property Name=\"teacherNumber\" Type=\"Edm.String\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_TeacherNumber\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <Property Name=\"externalId\" Type=\"Edm.String\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SyncSource_TeacherId\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "      </ComplexType>" +
                  "      <EntityType Name=\"educationClass\" ags:IsMaster=\"true\" ags:FacadedServiceNamespace=\"Microsoft.DirectoryServices\" ags:WorkloadName=\"Group\"  ags:ApprovalData=\"Microsoft.ApprovalManagement, https://approvalmanagement.activedirectory.windowsazure.com/odata/[tenantId]/ApprovalNotifications/, *\" ags:IncludePropertiesOnCreate='{\"mailEnabled\": \"true\", \"securityEnabled\": \"false\", \"groupTypes\": [\"Unified\"], \"extension_fe2174665583431c953114ff7268b7b3_Education_ObjectType\": \"Section\", \"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByAppId\": \"@appid@\", \"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByUserId\": \"@userid@\"}' ags:Tags=\"UnifiedGroupNotification\" ags:WorkloadQueryFilter=\"extension_fe2174665583431c953114ff7268b7b3_Education_ObjectType eq 'Section'\">" +
                  "        <Property Name=\"displayName\" ags:WorkloadName=\"displayName\" Type=\"Edm.String\" Nullable=\"false\" />" +
                  "        <Property Name=\"description\" ags:WorkloadName=\"description\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"mailNickname\" ags:WorkloadName=\"mailNickname\" Type=\"Edm.String\" Nullable=\"false\" />" +
                  "        <Property Name=\"period\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_Period\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"classNumber\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SectionNumber\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"externalName\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SectionName\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"externalId\"  ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SyncSource_SectionId\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"externalSource\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SyncSource\" Type=\"Microsoft.Education.Rostering.Api.educationExternalSource\" Nullable=\"false\" />" +
                  "        <Property Name=\"createdBy\" Nullable=\"true\" Type=\"Microsoft.Education.Rostering.Api.identitySet\" ags:IsTransparentContainer=\"true\" />" +
                  "        <Property Name=\"createdBy.application.displayName\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByAppDisplayName\" />" +
                  "        <Property Name=\"createdBy.application.id\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByAppId\" />" +
                  "        <Property Name=\"createdBy.user.displayName\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByUserDisplayName\" />" +
                  "        <Property Name=\"createdBy.user.id\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByUserId\" />" +
                  "        <NavigationProperty Name=\"schools\" Type=\"Collection(Microsoft.Education.Rostering.Api.educationSchool)\" ContainsTarget=\"false\" ags:WorkloadName=\"memberOf\"  ags:WorkloadQueryCast=\"true\" ags:WorkloadClientFilter=\"true\" />" +
                  "        <NavigationProperty Name=\"members\" Type=\"Collection(Microsoft.Education.Rostering.Api.educationUser)\" ContainsTarget=\"false\" ags:WorkloadName=\"members\" ags:WorkloadQueryCast=\"true\" ags:WorkloadClientFilter=\"true\" />" +
                  "        <NavigationProperty Name=\"teachers\" Type=\"Collection(Microsoft.Education.Rostering.Api.educationUser)\" ContainsTarget=\"false\" ags:WorkloadName=\"owners\" ags:WorkloadQueryCast=\"true\" ags:WorkloadClientFilter=\"true\" />" +
                  "      </EntityType>" +
                  "      <EntityType Name=\"educationOrganization\" ags:IsMaster=\"true\" Abstract=\"True\">" +
                  "        <Property Name=\"displayName\" ags:WorkloadName=\"displayName\" Type=\"Edm.String\" Nullable=\"false\" />" +
                  "        <Property Name=\"description\" ags:WorkloadName=\"description\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"externalSource\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SyncSource\" Type=\"Microsoft.Education.Rostering.Api.educationExternalSource\" Nullable=\"false\" />" +
                  "      </EntityType>" +
                  "      <EntityType Name=\"educationSchool\" ags:IsMaster=\"true\" BaseType=\"Microsoft.Education.Rostering.Api.educationOrganization\" ags:FacadedServiceNamespace=\"Microsoft.DirectoryServices\" ags:WorkloadName=\"AdministrativeUnit\" ags:WorkloadQueryFilter=\"extension_fe2174665583431c953114ff7268b7b3_Education_ObjectType eq 'School'\" ags:IncludePropertiesOnCreate='{\"extension_fe2174665583431c953114ff7268b7b3_Education_ObjectType\": \"School\", \"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByAppId\": \"@appid@\", \"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByUserId\": \"@userid@\"}'>" +
                  "        <Property Name=\"schoolZone\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SchoolZone\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"schoolPrincipalEmail\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SchoolPrincipalEmail\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"schoolPrincipalName\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SchoolPrincipalName\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"externalSchoolPrincipalId\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SchoolPrincipalSyncSourceId\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"lowestGrade\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_LowestGrade\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"highestGrade\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_HighestGrade\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"schoolNationalCenterForEducationStatisticsId\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SchoolNationalCenterForEducationStatisticsId\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"schoolNumber\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SchoolNumber\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"externalId\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_SyncSource_SchoolId\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"phone\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_Phone\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"fax\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_Fax\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <Property Name=\"createdBy\" Nullable=\"true\" Type=\"Microsoft.Education.Rostering.Api.identitySet\" ags:IsTransparentContainer=\"true\" />" +
                  "        <Property Name=\"createdBy.application.displayName\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByAppDisplayName\" />" +
                  "        <Property Name=\"createdBy.application.id\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByAppId\" />" +
                  "        <Property Name=\"createdBy.user.displayName\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByUserDisplayName\" />" +
                  "        <Property Name=\"createdBy.user.id\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_CreatedByUserId\" />" +
                  "        <Property Name=\"address\" Type=\"Microsoft.Education.Rostering.Api.physicalAddress\" ags:IsTransparentContainer=\"true\"/>" +
                  "        <Property Name=\"address.street\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_Address\" />" +
                  "        <Property Name=\"address.city\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_City\" />" +
                  "        <Property Name=\"address.state\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_State\" />" +
                  "        <Property Name=\"address.countryOrRegion\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_Country\" />" +
                  "        <Property Name=\"address.postalCode\" Type=\"Edm.String\" ags:TransparentContainerWorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_Zip\" />" +
                  "        <Property Name=\"stateId\" ags:WorkloadName=\"extension_fe2174665583431c953114ff7268b7b3_Education_StateId\" Type=\"Edm.String\" Nullable=\"true\" />" +
                  "        <NavigationProperty Name=\"classes\" Type=\"Collection(Microsoft.Education.Rostering.Api.educationClass)\" ContainsTarget=\"false\" ags:WorkloadName=\"members\"  ags:WorkloadQueryCast=\"true\" ags:WorkloadClientFilter=\"true\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "        <NavigationProperty Name=\"users\" Type=\"Collection(Microsoft.Education.Rostering.Api.educationUser)\" ContainsTarget=\"false\" ags:WorkloadName=\"members\"  ags:WorkloadQueryCast=\"true\" ags:WorkloadClientFilter=\"true\" ags:RequiredScopes=\"EduRoster.Read EduRoster.ReadWrite\" ags:RequiredRoles=\"EduRoster.Read.All EduRoster.ReadWrite.All\"/>" +
                  "      </EntityType>" +
                  "      <EntityContainer Name=\"EducationRosteringService\" ags:IsODataV3=\"true\" ags:AdditionalQueryParameters=\"api-version=AGSbeta-internal\">" +
                  "        <Singleton Name=\"education\" Type=\"Microsoft.Education.Rostering.Api.educationRoot\">" +
                  "          <NavigationPropertyBinding Path=\"classes/teachers\" Target=\"education/users\" />" +
                  "          <NavigationPropertyBinding Path=\"classes/members\" Target=\"education/users\" />" +
                  "          <NavigationPropertyBinding Path=\"classes/schools\" Target=\"education/schools\" />" +
                  "          <NavigationPropertyBinding Path=\"schools/classes\" Target=\"education/classes\" />" +
                  "          <NavigationPropertyBinding Path=\"schools/users\" Target=\"education/users\" />" +
                  "          <NavigationPropertyBinding Path=\"users/schools\" Target=\"education/schools\" />" +
                  "          <NavigationPropertyBinding Path=\"users/classes\" Target=\"education/classes\" />" +
                  "        </Singleton>" +
                  "      </EntityContainer>" +
                  "    </Schema>" +
                  "  </edmx:DataServices>" +
                  "</edmx:Edmx>";
            var model = CsdlReader.Parse(XElement.Parse(csdl).CreateReader());
        }

        [Fact]
        public void ReadNavigationPropertyPartnerTypeHierarchyTest()
        {
            var csdl =
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>" +
                "<edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\">" +
                    "<edmx:DataServices>" +
                        "<Schema Namespace=\"NS\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\">" +
                            "<EntityType Name=\"EntityTypeA1\">" +
                                "<Key><PropertyRef Name=\"ID\" /></Key>" +
                                "<Property Name=\"ID\" Type=\"Edm.Int32\" />" +
                                "<NavigationProperty Name=\"A1Nav\" Type=\"NS.EntityTypeB\" Nullable=\"false\" Partner=\"BNav1\" />" +
                            "</EntityType>" +
                            "<EntityType Name=\"EntityTypeA2\" BaseType=\"NS.EntityTypeA1\" />" +
                            "<EntityType Name=\"EntityTypeA3\" BaseType=\"NS.EntityTypeA2\">" +
                                "<NavigationProperty Name=\"A3Nav\" Type=\"NS.EntityTypeB\" Nullable=\"false\" Partner=\"BNav2\" />" +
                            "</EntityType>" +
                            "<EntityType Name=\"EntityTypeB\">" +
                                "<Key><PropertyRef Name=\"ID\" /></Key>" +
                                "<Property Name=\"ID\" Type=\"Edm.Int32\" />" +
                                "<NavigationProperty Name=\"BNav1\" Type=\"NS.EntityTypeA2\" Nullable=\"false\" Partner=\"A1Nav\" />" +
                                "<NavigationProperty Name=\"BNav2\" Type=\"NS.EntityTypeA3\" Nullable=\"false\" Partner=\"NS.EntityTypeA3/A3Nav\" />" +
                            "</EntityType>" +
                        "</Schema>" +
                    "</edmx:DataServices>" +
                "</edmx:Edmx>";
            var model = CsdlReader.Parse(XElement.Parse(csdl).CreateReader());
            var entityTypeA1 = (IEdmEntityType)model.FindDeclaredType("NS.EntityTypeA1");
            var entityTypeA2 = (IEdmEntityType)model.FindDeclaredType("NS.EntityTypeA2");
            var entityTypeA3 = (IEdmEntityType)model.FindDeclaredType("NS.EntityTypeA3");
            var entityTypeB = (IEdmEntityType)model.FindDeclaredType("NS.EntityTypeB");
            Assert.Equal("BNav1", ((IEdmNavigationProperty)entityTypeA2.FindProperty("A1Nav")).GetPartnerPath().Path);
            Assert.Equal(entityTypeB.FindProperty("BNav1"), ((IEdmNavigationProperty)entityTypeA2.FindProperty("A1Nav")).Partner);
            Assert.Equal("BNav2", ((IEdmNavigationProperty)entityTypeA3.FindProperty("A3Nav")).GetPartnerPath().Path);
            Assert.Equal(entityTypeB.FindProperty("BNav2"), ((IEdmNavigationProperty)entityTypeA3.FindProperty("A3Nav")).Partner);
            Assert.Equal("A1Nav", ((IEdmNavigationProperty)entityTypeB.FindProperty("BNav1")).GetPartnerPath().Path);
            Assert.Equal(entityTypeA2.FindProperty("A1Nav"), ((IEdmNavigationProperty)entityTypeB.FindProperty("BNav1")).Partner);
            Assert.Equal("NS.EntityTypeA3/A3Nav", ((IEdmNavigationProperty)entityTypeB.FindProperty("BNav2")).GetPartnerPath().Path);
            Assert.Equal(entityTypeA3.FindProperty("A3Nav"), ((IEdmNavigationProperty)entityTypeB.FindProperty("BNav2")).Partner);
        }

        [Fact]
        public void ParsingValidXmlWithNoReferencesShouldSucceed()
        {
            this.RunValidTest(CsdlReader.Parse);
        }

        [Fact]
        public void ParsingInvalidXmlWithNoReferencesShouldThrow()
        {
            this.RunInvalidTest(CsdlReader.Parse);
        }

        [Fact]
        public void ParsingvalidXmlWithNavigationPropertyInComplex()
        {
            string complexWithNav =
"<?xml version=\"1.0\" encoding=\"utf-16\"?><edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\">" +
                "<edmx:DataServices>" +
                "<Schema Namespace=\"DefaultNs\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\">" +
                "<EntityType Name=\"Person\">" +
                    "<Key><PropertyRef Name=\"UserName\" /></Key>" +
                    "<Property Name=\"UserName\" Type=\"Edm.String\" Nullable=\"false\" />" +
                    "<Property Name=\"HomeAddress\" Type=\"DefaultNs.Address\" Nullable=\"false\" />" +
                    "<Property Name=\"WorkAddress\" Type=\"DefaultNs.Address\" Nullable=\"false\" />" +
                    "<Property Name=\"Addresses\" Type=\"Collection(DefaultNs.Address)\" Nullable=\"false\" />" +
                "</EntityType>" +
                "<EntityType Name=\"City\">" +
                    "<Key><PropertyRef Name=\"Name\" /></Key>" +
                    "<Property Name=\"Name\" Type=\"Edm.String\" Nullable=\"false\" />" +
                "</EntityType>" +
                "<EntityType Name=\"CountryOrRegion\">" +
                    "<Key><PropertyRef Name=\"Name\" /></Key>" +
                    "<Property Name=\"Name\" Type=\"Edm.String\" Nullable=\"false\" />" +
                "</EntityType>" +
                "<ComplexType Name=\"Address\">" +
                    "<Property Name=\"Id\" Type=\"Edm.Int32\" Nullable=\"false\" />" +
                    "<NavigationProperty Name=\"City\" Type=\"DefaultNs.City\" Nullable=\"false\" />" +
                "</ComplexType>" +
                "<ComplexType Name=\"WorkAddress\" BaseType=\"DefaultNs.Address\">" +
                    "<NavigationProperty Name=\"CountryOrRegion\" Type=\"DefaultNs.CountryOrRegion\" Nullable=\"false\" /><" +
                "/ComplexType>" +
                "<EntityContainer Name=\"Container\">" +
                "<EntitySet Name=\"People\" EntityType=\"DefaultNs.Person\">" +
                    "<NavigationPropertyBinding Path=\"HomeAddress/City\" Target=\"Cities\" />" +
                    "<NavigationPropertyBinding Path=\"Addresses/City\" Target=\"Cities\" />" +
                    "<NavigationPropertyBinding Path=\"WorkAddress/DefaultNs.WorkAddress/CountryOrRegion\" Target=\"CountriesOrRegions\" />" +
                "</EntitySet>" +
                "<EntitySet Name=\"Cities\" EntityType=\"DefaultNs.City\" />" +
                "<EntitySet Name=\"CountriesOrRegions\" EntityType=\"DefaultNs.CountryOrRegion\" />" +
                "</EntityContainer></Schema>" +
                "</edmx:DataServices>" +
                "</edmx:Edmx>";

            var model = CsdlReader.Parse(XElement.Parse(complexWithNav).CreateReader());
            var people = model.EntityContainer.FindEntitySet("People");
            var address = model.FindType("DefaultNs.Address") as IEdmStructuredType;
            var workAddress = model.FindType("DefaultNs.WorkAddress") as IEdmStructuredType;
            var city = address.FindProperty("City") as IEdmNavigationProperty;
            var countryOrRegion = workAddress.FindProperty("CountryOrRegion") as IEdmNavigationProperty;
            var cities = model.EntityContainer.FindEntitySet("Cities");
            var countriesOrRegions = model.EntityContainer.FindEntitySet("CountriesOrRegions");
            var navigationTarget = people.FindNavigationTarget(city, new EdmPathExpression("HomeAddress/City"));
            Assert.Equal(navigationTarget, cities);
            navigationTarget = people.FindNavigationTarget(city, new EdmPathExpression("Addresses/City"));
            Assert.Equal(navigationTarget, cities);
            navigationTarget = people.FindNavigationTarget(countryOrRegion, new EdmPathExpression("WorkAddress/DefaultNs.WorkAddress/CountryOrRegion"));
            Assert.Equal(navigationTarget, countriesOrRegions);
        }

        [Fact]
        public void ParsingXmlWithCollectionOfNavOnComplex()
        {
            string complexWithNav = "<?xml version=\"1.0\" encoding=\"utf-16\"?><edmx:Edmx Version=\"4.0\" xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\">" +
                              "<edmx:DataServices><Schema Namespace=\"DefaultNs\" xmlns=\"http://docs.oasis-open.org/odata/ns/edm\">" +
                              "<EntityType Name=\"EntityType\">" +
                                  "<Key><PropertyRef Name=\"ID\" /></Key>" +
                                  "<Property Name=\"ID\" Type=\"Edm.String\" Nullable=\"false\" />" +
                                  "<Property Name=\"Complex\" Type=\"DefaultNs.ComplexType\" Nullable=\"false\" />" +
                              "</EntityType>" +
                              "<EntityType Name=\"NavEntityType\">" +
                                  "<Key><PropertyRef Name=\"ID\" /></Key>" +
                                  "<Property Name=\"ID\" Type=\"Edm.String\" Nullable=\"false\" />" +
                              "</EntityType>" +
                              "<ComplexType Name=\"ComplexType\">" +
                                  "<Property Name=\"Prop1\" Type=\"Edm.Int32\" Nullable=\"false\" />" +
                                  "<NavigationProperty Name=\"CollectionOfNav\" Type=\"Collection(DefaultNs.NavEntityType)\" />" +
                              "</ComplexType>" +
                              "<EntityContainer Name=\"Container\">" +
                              "<EntitySet Name=\"Entities\" EntityType=\"DefaultNs.EntityType\">" +
                                "<NavigationPropertyBinding Path=\"Complex/CollectionOfNav\" Target=\"NavEntities\" />" +
                              "</EntitySet>" +
                              "<EntitySet Name=\"NavEntities\" EntityType=\"DefaultNs.NavEntityType\" />" +
                              "</EntityContainer>" +
                              "</Schema>" +
                              "</edmx:DataServices>" +
                              "</edmx:Edmx>";

            var model = CsdlReader.Parse(XElement.Parse(complexWithNav).CreateReader());
            var people = model.EntityContainer.FindEntitySet("Entities");
            var address = model.FindType("DefaultNs.ComplexType") as IEdmStructuredType;
            var city = address.FindProperty("CollectionOfNav") as IEdmNavigationProperty;
            var cities = model.EntityContainer.FindEntitySet("NavEntities");
            var navigationTarget = people.FindNavigationTarget(city, new EdmPathExpression("Complex/CollectionOfNav"));
            Assert.Equal(navigationTarget, cities);
        }

        [Fact]
        public void ParsingOptionalParametersShouldSucceed()
        {
            string csdl =
        "<edmx:Edmx xmlns:edmx=\"http://docs.oasis-open.org/odata/ns/edmx\" Version=\"4.0\">" +
          "<edmx:DataServices>" +
            "<Schema xmlns=\"http://docs.oasis-open.org/odata/ns/edm\" Namespace=\"test\">" +
              "<Function Name=\"TestFunction\">" +
                "<Parameter Name = \"requiredParam\" Type=\"Edm.String\"/>" +
                "<Parameter Name = \"optionalParam\" Type=\"Edm.String\">" +
                    "<Annotation Term=\"Org.OData.Core.V1.OptionalParameter\"/>" +
                "</Parameter>" +
                "<Parameter Name = \"optionalParamWithDefault\" Type=\"Edm.String\">" +
                    "<Annotation Term=\"Org.OData.Core.V1.OptionalParameter\">" +
                      "<Record>" +
                        "<PropertyValue Property=\"DefaultValue\" String=\"Smith\"/>" +
                      "</Record>" +
                    "</Annotation>" +
                "</Parameter>" +
                "<ReturnType Type=\"Edm.Boolean\"/>" +
              "</Function>" +
              "<EntityContainer Name=\"Default\">" +
                "<FunctionImport Name=\"TestFunction\" Function=\"test.TestFunction\"/>" +
              "</EntityContainer>" +
            "</Schema>" +
          "</edmx:DataServices>" +
        "</edmx:Edmx>";

            var model = CsdlReader.Parse(XElement.Parse(csdl).CreateReader());
            var function = model.FindDeclaredOperations("test.TestFunction").FirstOrDefault();
            Assert.NotNull(function);
            var requiredParam = function.Parameters.Where(p => p.Name == "requiredParam").FirstOrDefault();
            Assert.NotNull(requiredParam);
            Assert.Null(requiredParam as IEdmOptionalParameter);
            IEdmOptionalParameter optionalParam = function.Parameters.Where(p => p.Name == "optionalParam").FirstOrDefault() as IEdmOptionalParameter;
            Assert.NotNull(optionalParam);
            Assert.True(String.IsNullOrEmpty(optionalParam.DefaultValueString));
            IEdmOptionalParameter optionalParamWithDefault = function.Parameters.Where(p => p.Name == "optionalParamWithDefault").FirstOrDefault() as IEdmOptionalParameter;
            Assert.NotNull(optionalParamWithDefault);
            Assert.Equal(optionalParamWithDefault.DefaultValueString, "Smith");
        }

        [Fact]
        public void ParsingValidXmlWithOneReferencesShouldSucceed()
        {
            this.RunValidTest(r => CsdlReader.Parse(r, EdmCoreModel.Instance));
        }

        [Fact]
        public void ParsingInvalidXmlWithOneReferencesShouldThrow()
        {
            this.RunInvalidTest(r => CsdlReader.Parse(r, EdmCoreModel.Instance));
        }

        [Fact]
        public void ParsingValidXmlWithManyReferencesShouldSucceed()
        {
            this.RunValidTest(r => CsdlReader.Parse(r, new IEdmModel[] { EdmCoreModel.Instance }));
        }

        [Fact]
        public void ParsingInvalidXmlWithManyReferencesShouldThrow()
        {
            this.RunInvalidTest(r => CsdlReader.Parse(r, new IEdmModel[] { EdmCoreModel.Instance }));
        }

        [Fact]
        public void ParsingInvalidXmlWithMultipleEntityContainersShouldThrow()
        {
            string EdmxwithMultipleEntityContainers = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""Test"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityContainer Name=""Container1"" />
      <EntityContainer Name=""Container2"" />
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
            Action parseAction = () => CsdlReader.Parse(XElement.Parse(EdmxwithMultipleEntityContainers).CreateReader());
            parseAction.ShouldThrow<EdmParseException>().Where(e => e.Message.Contains(
                Strings.CsdlParser_MetadataDocumentCannotHaveMoreThanOneEntityContainer)).And.Errors.Should().HaveCount(1);
        }

        [Fact]
        public void ParsingNavigationPropertyWithEdmEntityTypeWorks()
        {
            string navigationProperty =
                @"<NavigationProperty Name=""EntityNavigationProperty"" Type=""Edm.EntityType"" />";

            IEdmModel model = GetEdmModel(properties: navigationProperty);
            IEnumerable<EdmError> errors;
            Assert.True(model.Validate(out errors));

            var customer = model.SchemaElements.OfType<IEdmEntityType>().FirstOrDefault(c => c.Name == "Customer");
            Assert.NotNull(customer);
            var navProperty = customer.DeclaredNavigationProperties().FirstOrDefault(c => c.Name == "EntityNavigationProperty");
            Assert.NotNull(navProperty);
            Assert.True(navProperty.Type.IsNullable);
            Assert.Same(EdmCoreModel.Instance.GetEntityType(), navProperty.Type.Definition);

            var address = model.SchemaElements.OfType<IEdmComplexType>().FirstOrDefault(c => c.Name == "Address");
            Assert.NotNull(address);
            navProperty = address.DeclaredNavigationProperties().FirstOrDefault(c => c.Name == "EntityNavigationProperty");
            Assert.NotNull(navProperty);
            Assert.True(navProperty.Type.IsNullable);
            Assert.Same(EdmCoreModel.Instance.GetEntityType(), navProperty.Type.Definition);
        }

        [Fact]
        public void ParsingPropertyWithEdmComplexTypeWorks()
        {
            string complexProperty =
                @"<Property Name=""ComplexProperty"" Type=""Edm.ComplexType"" />";

            IEdmModel model = GetEdmModel(properties: complexProperty);
            IEnumerable<EdmError> errors;
            Assert.True(model.Validate(out errors));

            var customer = model.SchemaElements.OfType<IEdmEntityType>().FirstOrDefault(c => c.Name == "Customer");
            Assert.NotNull(customer);
            var property = customer.DeclaredProperties.FirstOrDefault(c => c.Name == "ComplexProperty");
            Assert.NotNull(property);
            Assert.True(property.Type.IsNullable);
            Assert.Same(EdmCoreModel.Instance.GetComplexType(), property.Type.Definition);

            var address = model.SchemaElements.OfType<IEdmComplexType>().FirstOrDefault(c => c.Name == "Address");
            Assert.NotNull(address);
            property = address.DeclaredProperties.FirstOrDefault(c => c.Name == "ComplexProperty");
            Assert.NotNull(property);
            Assert.True(property.Type.IsNullable);
            Assert.Same(EdmCoreModel.Instance.GetComplexType(), property.Type.Definition);
        }

        [Fact]
        public void ParsingPropertyWithEdmPrimitiveTypeWorks()
        {
            string complexProperty =
                @"<Property Name=""PrimitiveProperty"" Type=""Edm.PrimitiveType"" Nullable=""false"" />";

            IEdmModel model = GetEdmModel(properties: complexProperty);
            IEnumerable<EdmError> errors;
            Assert.True(model.Validate(out errors));

            var customer = model.SchemaElements.OfType<IEdmEntityType>().FirstOrDefault(c => c.Name == "Customer");
            Assert.NotNull(customer);
            var property = customer.DeclaredProperties.FirstOrDefault(c => c.Name == "PrimitiveProperty");
            Assert.NotNull(property);
            Assert.False(property.Type.IsNullable);
            Assert.Same(EdmCoreModel.Instance.GetPrimitiveType(), property.Type.Definition);

            var address = model.SchemaElements.OfType<IEdmComplexType>().FirstOrDefault(c => c.Name == "Address");
            Assert.NotNull(address);
            property = address.DeclaredProperties.FirstOrDefault(c => c.Name == "PrimitiveProperty");
            Assert.NotNull(property);
            Assert.False(property.Type.IsNullable);
            Assert.Same(EdmCoreModel.Instance.GetPrimitiveType(), property.Type.Definition);
        }

        [Fact]
        public void ParsingTermPropertyWithEdmPathTypeWorks()
        {
            string termTypes =
                @"<ComplexType Name=""SelectType"" >
                    <Property Name=""DefaultSelect"" Type=""Collection(Edm.PropertyPath)"" />
                    <Property Name=""DefaultHidden"" Type=""Collection(Edm.NavigationPropertyPath)"" Nullable=""false"" />
                  </ComplexType>
                  <Term Name=""MyTerm"" Type=""NS.SelectType"" />";

            IEdmModel model = GetEdmModel(types: termTypes);
            IEnumerable<EdmError> errors;
            Assert.True(model.Validate(out errors));

            var complex = model.SchemaElements.OfType<IEdmComplexType>().FirstOrDefault(c => c.Name == "SelectType");
            Assert.NotNull(complex);
            var property = complex.DeclaredProperties.FirstOrDefault(c => c.Name == "DefaultSelect");
            Assert.NotNull(property);
            Assert.True(property.Type.IsNullable);
            Assert.True(property.Type.IsCollection());
            Assert.Equal(EdmTypeKind.Path, property.Type.AsCollection().ElementType().TypeKind());
            Assert.Same(EdmCoreModel.Instance.GetPathType(EdmPathTypeKind.PropertyPath), property.Type.AsCollection().ElementType().Definition);

            property = complex.DeclaredProperties.FirstOrDefault(c => c.Name == "DefaultHidden");
            Assert.NotNull(property);
            Assert.False(property.Type.IsNullable);
            Assert.True(property.Type.IsCollection());
            Assert.Equal(EdmTypeKind.Path, property.Type.AsCollection().ElementType().TypeKind());
            Assert.Same(EdmCoreModel.Instance.GetPathType(EdmPathTypeKind.NavigationPropertyPath), property.Type.AsCollection().ElementType().Definition);
        }

        [Fact]
        public void ParsingPropertyWithEdmPathTypeWorksButValidationFailed()
        {
            string properties =
                @"<Property Name=""PathProperty"" Type=""Collection(Edm.PropertyPath)"" />";

            IEdmModel model = GetEdmModel(properties: properties);

            var customer = model.SchemaElements.OfType<IEdmEntityType>().FirstOrDefault(c => c.Name == "Customer");
            Assert.NotNull(customer);
            var property = customer.DeclaredProperties.FirstOrDefault(c => c.Name == "PathProperty");
            Assert.NotNull(property);
            Assert.True(property.Type.IsNullable);
            Assert.True(property.Type.IsCollection());
            Assert.Equal(EdmTypeKind.Path, property.Type.AsCollection().ElementType().TypeKind());
            Assert.Same(EdmCoreModel.Instance.GetPathType(EdmPathTypeKind.PropertyPath), property.Type.AsCollection().ElementType().Definition);

            IEnumerable<EdmError> errors;
            Assert.False(model.Validate(out errors));
            var error = Assert.Single(errors);
            Assert.NotNull(error);
            Assert.Equal(EdmErrorCode.DeclaringTypeOfNavigationSourceCannotHavePathProperty, error.ErrorCode);
        }

        [Fact]
        public void ParsingRecursivePropertyWithEdmPathTypeWorksButValidationFailed()
        {
            string properties =
                @"<Property Name=""PathProperty"" Type=""Collection(Edm.PropertyPath)"" />
                  <Property Name=""ComplexProperty"" Type=""NS.Address"" />";

            IEdmModel model = GetEdmModel(properties: properties);

            var address = model.SchemaElements.OfType<IEdmComplexType>().FirstOrDefault(c => c.Name == "Address");
            Assert.NotNull(address);
            var property = address.DeclaredProperties.FirstOrDefault(c => c.Name == "ComplexProperty");
            Assert.NotNull(property);
            Assert.True(property.Type.IsNullable);
            Assert.Equal(EdmTypeKind.Complex, property.Type.TypeKind());

            IEnumerable<EdmError> errors;
            Assert.False(model.Validate(out errors));
            var error = Assert.Single(errors);
            Assert.NotNull(error);
            Assert.Equal(EdmErrorCode.DeclaringTypeOfNavigationSourceCannotHavePathProperty, error.ErrorCode);
        }

        [Fact]
        public void ParsingNavigationPropertyWithEdmPathTypeWorksButValidationFailed()
        {
            string properties =
                @"<NavigationProperty Name=""NavigationProperty"" Type=""Collection(NS.Customer)"" />
                  <Property Name=""PathProperty"" Type=""Collection(Edm.AnnotationPath)"" />";

            IEdmModel model = GetEdmModel(properties: properties);

            var customer = model.SchemaElements.OfType<IEdmEntityType>().FirstOrDefault(c => c.Name == "Customer");
            Assert.NotNull(customer);
            var navProperty = customer.DeclaredNavigationProperties().FirstOrDefault(c => c.Name == "NavigationProperty");
            Assert.NotNull(navProperty);

            IEnumerable<EdmError> errors;
            Assert.False(model.Validate(out errors));
            Assert.NotNull(errors);
            Assert.Equal(3, errors.Count());

            Assert.Equal(new[] {EdmErrorCode.TypeOfNavigationPropertyCannotHavePathProperty,
                EdmErrorCode.TypeOfNavigationPropertyCannotHavePathProperty,
                EdmErrorCode.DeclaringTypeOfNavigationSourceCannotHavePathProperty}, errors.Select(e => e.ErrorCode));
        }

        private void RunValidTest(Func<XmlReader, IEdmModel> parse)
        {
            var result = parse(this.validReader);
            result.Should().NotBeNull();
            result.EntityContainer.FullName().Should().Be("Test.Container");
        }

        private void RunInvalidTest(Func<XmlReader, IEdmModel> parse)
        {
            Action parseAction = () => parse(this.invalidReader);
            parseAction.ShouldThrow<EdmParseException>().WithMessage(ErrorStrings.EdmParseException_ErrorsEncounteredInEdmx(ErrorMessage)).And.Errors.Should().OnlyContain(e => e.ToString() == ErrorMessage);
        }

        private static IEdmModel GetEdmModel(string types = "", string properties = "")
        {
            const string template = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""NS"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""Customer"">
        <Key>
          <PropertyRef Name=""ID"" />
        </Key>
        <Property Name=""ID"" Type=""Edm.Int32"" Nullable=""false"" />
        {1}
      </EntityType>
      <ComplexType Name=""Address"">
        <Property Name=""Street"" Type=""Edm.String"" />
        {1}
      </ComplexType>
      {0}
      <EntityContainer Name =""Default"">
         <EntitySet Name=""Customers"" EntityType=""NS.Customer"" />
      </EntityContainer>
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
            string modelText = string.Format(template, types, properties);

            IEdmModel model;
            IEnumerable<EdmError> errors;

            bool result = CsdlReader.TryParse(XElement.Parse(modelText).CreateReader(), out model, out errors);
            Assert.True(result);
            return model;
        }

        [Fact]
        public void ParsingBaseAndDerivedTypeWithSameAnnotationWorksButValidationSuccessful()
        {
            string annotations =@"
            <Annotations Target=""NS.Base"">
                <Annotation Term=""Org.OData.Core.V1.Description"" String=""Base description"" />
            </Annotations>
            <Annotations Target=""NS.Derived"">
                <Annotation Term=""Org.OData.Core.V1.Description"" String=""Derived description"" />
            </Annotations>";

            IEdmModel model = GetInheritanceEdmModel(annotations);

            var edmType = model.SchemaElements.OfType<IEdmEntityType>().FirstOrDefault(c => c.Name == "Base");
            Assert.NotNull(edmType);
            Assert.Equal("Base description", model.GetDescriptionAnnotation(edmType));

            edmType = model.SchemaElements.OfType<IEdmEntityType>().FirstOrDefault(c => c.Name == "Derived");
            Assert.NotNull(edmType);
            Assert.Equal("Derived description", model.GetDescriptionAnnotation(edmType));

            IEnumerable<EdmError> errors;
            Assert.True(model.Validate(out errors));
        }

        [Fact]
        public void ParsingDerivedTypeWithDuplicatedAnnotationsWorksButValidationFailed()
        {
            string annotations = @"
            <Annotations Target=""NS.Derived"">
                <Annotation Term=""Org.OData.Core.V1.Description"" String=""Derived description 1"" />
            </Annotations>
            <Annotations Target=""NS.Derived"">
                <Annotation Term=""Org.OData.Core.V1.Description"" String=""Derived description 2"" />
            </Annotations>";

            IEdmModel model = GetInheritanceEdmModel(annotations);

            var edmType = model.SchemaElements.OfType<IEdmEntityType>().FirstOrDefault(c => c.Name == "Derived");
            Assert.NotNull(edmType);
            var descriptions = model.FindVocabularyAnnotations<IEdmVocabularyAnnotation>(edmType, CoreVocabularyModel.DescriptionTerm);
            Assert.Equal(new [] { "Derived description 1", "Derived description 2" },
                descriptions.Select(d => d.Value as IEdmStringConstantExpression).Select(e => e.Value));

            IEnumerable<EdmError> errors;
            Assert.False(model.Validate(out errors));
            EdmError error = Assert.Single(errors);
            Assert.NotNull(error);
            Assert.Equal(EdmErrorCode.DuplicateAnnotation, error.ErrorCode);
            Assert.Equal("The annotated element 'NS.Derived' has multiple annotations with the term 'Org.OData.Core.V1.Description' and the qualifier ''.", error.ErrorMessage);
        }

        private static IEdmModel GetInheritanceEdmModel(string annotation)
        {
            const string template = @"<edmx:Edmx Version=""4.0"" xmlns:edmx=""http://docs.oasis-open.org/odata/ns/edmx"">
  <edmx:DataServices>
    <Schema Namespace=""NS"" xmlns=""http://docs.oasis-open.org/odata/ns/edm"">
      <EntityType Name=""Base"">
        <Key>
          <PropertyRef Name=""ID"" />
        </Key>
        <Property Name=""ID"" Type=""Edm.Int32"" Nullable=""false"" />
      </EntityType>
      <EntityType Name=""Derived"" BaseType=""NS.Base"" />
      {0}
    </Schema>
  </edmx:DataServices>
</edmx:Edmx>";
            string modelText = string.Format(template, annotation);

            IEdmModel model;
            IEnumerable<EdmError> errors;

            bool result = CsdlReader.TryParse(XElement.Parse(modelText).CreateReader(), out model, out errors);
            Assert.True(result);
            return model;
        }
    }
}
