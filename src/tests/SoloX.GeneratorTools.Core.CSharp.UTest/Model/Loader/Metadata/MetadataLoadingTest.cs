﻿// ----------------------------------------------------------------------
// <copyright file="MetadataLoadingTest.cs" company="Xavier Solau">
// Copyright © 2021 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.CodeAnalysis;
using Moq;
using SoloX.GeneratorTools.Core.CSharp.Generator.Attributes;
using SoloX.GeneratorTools.Core.CSharp.Model.Impl;
using SoloX.GeneratorTools.Core.CSharp.Model.Impl.Loader.Metadata;
using SoloX.GeneratorTools.Core.CSharp.Model.Resolver;
using SoloX.GeneratorTools.Core.CSharp.Model.Use.Impl;
using SoloX.GeneratorTools.Core.CSharp.UTest.Resources.Model.Basic;
using SoloX.GeneratorTools.Core.CSharp.UTest.Utils;
using Xunit;
using Xunit.Abstractions;

namespace SoloX.GeneratorTools.Core.CSharp.UTest.Model.Loader.Metadata
{
    public class MetadataLoadingTest
    {
        private readonly ITestOutputHelper testOutputHelper;

        public MetadataLoadingTest(ITestOutputHelper testOutputHelper)
        {
            this.testOutputHelper = testOutputHelper;
        }

        [Theory]
        [InlineData(typeof(SimpleClass))]
        [InlineData(typeof(SimpleClassWithBase))]
        [InlineData(typeof(SimpleClassWithGenericBase))]
        [InlineData(typeof(GenericClass<>))]
        [InlineData(typeof(GenericClassWithBase<>))]
        [InlineData(typeof(GenericClassWithGenericBase<>))]
        public void BasicMetadataLoadingTest(Type type)
        {
            var assemblyPath = type.Assembly.Location;

            using var portableExecutableReader = new PEReader(File.OpenRead(assemblyPath));

            var metadataReader = portableExecutableReader.GetMetadataReader();
            var typeDefinitionHandle = GetTypeDefinitionHandle(type, metadataReader);

            var declaration = DeclarationHelper.CreateDeclarationFactory(this.testOutputHelper)
                .CreateClassDeclaration(metadataReader, typeDefinitionHandle, assemblyPath);

            Assert.NotNull(declaration);
            Assert.Equal(
                MetadataGenericDeclarationLoader<SyntaxNode>.GetNameWithoutGeneric(metadataReader, typeDefinitionHandle),
                declaration.Name);

            Assert.NotNull(declaration.SyntaxNodeProvider);
            Assert.NotNull(declaration.SyntaxNodeProvider.SyntaxNode);

            Assert.Null(declaration.GenericParameters);
            Assert.Null(declaration.Extends);
            Assert.Null(declaration.Members);

            var classDeclaration = Assert.IsType<ClassDeclaration>(declaration);

            var declarationResolverMock = new Mock<IDeclarationResolver>();
            classDeclaration.DeepLoad(declarationResolverMock.Object);

            Assert.NotNull(declaration.GenericParameters);
            Assert.NotNull(declaration.Extends);
            Assert.NotNull(declaration.Members);

            if (type.IsGenericTypeDefinition)
            {
                Assert.NotEmpty(declaration.GenericParameters);

                var typeParams = type.GetTypeInfo().GenericTypeParameters;
                Assert.Equal(typeParams.Length, declaration.GenericParameters.Count);

                Assert.Equal(typeParams[0].Name, declaration.GenericParameters.First().Name);
            }
            else
            {
                Assert.Empty(declaration.GenericParameters);
            }
        }

        [Theory]
        [InlineData(typeof(GenericClassWithBase<>), nameof(SimpleClass))]
        [InlineData(typeof(GenericClassWithGenericBase<>), nameof(GenericClass<object>))]
        public void LoadExtendsTest(Type type, string baseClassName)
        {
            var assemblyPath = type.Assembly.Location;

            using var portableExecutableReader = new PEReader(File.OpenRead(assemblyPath));

            var metadataReader = portableExecutableReader.GetMetadataReader();
            var typeDefinitionHandle = GetTypeDefinitionHandle(type, metadataReader);

            var declaration = DeclarationHelper.CreateDeclarationFactory(this.testOutputHelper)
                .CreateClassDeclaration(metadataReader, typeDefinitionHandle, assemblyPath);

            var classDeclaration = Assert.IsType<ClassDeclaration>(declaration);

            var declarationResolverMock = new Mock<IDeclarationResolver>();
            classDeclaration.DeepLoad(declarationResolverMock.Object);

            Assert.NotEmpty(classDeclaration.Extends);

            var baseClassDefinition = classDeclaration.Extends.First().Declaration;

            Assert.NotNull(baseClassDefinition);
            Assert.Equal(baseClassName, baseClassDefinition.Name);
        }

        [Theory]
        [InlineData(typeof(ClassWithProperties), false)]
        [InlineData(typeof(ClassWithArrayProperties), true)]
        public void LoadPropertyListTest(Type type, bool isArray)
        {
            var assemblyPath = type.Assembly.Location;

            using var portableExecutableReader = new PEReader(File.OpenRead(assemblyPath));

            var metadataReader = portableExecutableReader.GetMetadataReader();
            var typeDefinitionHandle = GetTypeDefinitionHandle(type, metadataReader);

            var declarationFactory = DeclarationHelper.CreateDeclarationFactory(this.testOutputHelper);
            var declaration = declarationFactory.CreateClassDeclaration(metadataReader, typeDefinitionHandle, assemblyPath);
            var simpleClassDeclaration = declarationFactory.CreateClassDeclaration(typeof(SimpleClass));

            var classDeclaration = Assert.IsType<ClassDeclaration>(declaration);

            var declarationResolverMock = new Mock<IDeclarationResolver>();
            declarationResolverMock.Setup(r => r.Resolve(typeof(SimpleClass))).Returns(simpleClassDeclaration);
            classDeclaration.DeepLoad(declarationResolverMock.Object);

            Assert.NotEmpty(classDeclaration.Properties);
            Assert.Equal(2, classDeclaration.Properties.Count);

            var mClass = Assert.Single(declaration.Members.Where(m => m.Name == nameof(ClassWithProperties.PropertyClass)));
            var pClass = Assert.IsType<PropertyDeclaration>(mClass);

            Assert.IsType<GenericDeclarationUse>(pClass.PropertyType);
            Assert.Equal(nameof(SimpleClass), pClass.PropertyType.Declaration.Name);

            var mInt = Assert.Single(declaration.Members.Where(m => m.Name == nameof(ClassWithProperties.PropertyInt)));
            var pInt = Assert.IsType<PropertyDeclaration>(mInt);

            Assert.IsType<PredefinedDeclarationUse>(pInt.PropertyType);
            Assert.Equal("int", pInt.PropertyType.Declaration.Name);

            if (isArray)
            {
                Assert.NotNull(pClass.PropertyType.ArraySpecification);
                Assert.NotNull(pInt.PropertyType.ArraySpecification);
            }
            else
            {
                Assert.Null(pClass.PropertyType.ArraySpecification);
                Assert.Null(pInt.PropertyType.ArraySpecification);
            }
        }

        [Theory]
        [InlineData(typeof(ClassWithMethods), false)]
        [InlineData(typeof(ClassWithGenericMethods), true)]
        public void LoadMethodListTest(Type type, bool isGeneric)
        {
            var assemblyPath = type.Assembly.Location;

            using var portableExecutableReader = new PEReader(File.OpenRead(assemblyPath));

            var metadataReader = portableExecutableReader.GetMetadataReader();
            var typeDefinitionHandle = GetTypeDefinitionHandle(type, metadataReader);

            var declarationFactory = DeclarationHelper.CreateDeclarationFactory(this.testOutputHelper);
            var declaration = declarationFactory.CreateClassDeclaration(metadataReader, typeDefinitionHandle, assemblyPath);
            var simpleClassDeclaration = declarationFactory.CreateClassDeclaration(typeof(SimpleClass));

            var decl = Assert.IsType<ClassDeclaration>(declaration);

            var declarationResolverMock = new Mock<IDeclarationResolver>();
            declarationResolverMock.Setup(r => r.Resolve(typeof(SimpleClass))).Returns(simpleClassDeclaration);
            decl.DeepLoad(declarationResolverMock.Object);

            Assert.NotEmpty(decl.Methods);
            Assert.Equal(2, decl.Methods.Count);

            var mClass = Assert.Single(decl.Members.Where(m => m.Name == nameof(ClassWithMethods.ThisIsABasicMethod)));
            var methodClass = Assert.IsType<MethodDeclaration>(mClass);
            Assert.IsType<GenericDeclarationUse>(methodClass.ReturnType);
            Assert.Equal(nameof(SimpleClass), methodClass.ReturnType.Declaration.Name);

            Assert.Empty(methodClass.Parameters);

            var mInt = Assert.Single(decl.Members.Where(m => m.Name == nameof(ClassWithMethods.ThisIsAMethodWithParameters)));
            var methodInt = Assert.IsType<MethodDeclaration>(mInt);
            Assert.IsType<PredefinedDeclarationUse>(methodInt.ReturnType);
            Assert.Equal("int", methodInt.ReturnType.Declaration.Name);

            Assert.NotEmpty(methodInt.Parameters);
            Assert.Equal(3, methodInt.Parameters.Count);

            if (isGeneric)
            {
                Assert.NotNull(methodClass.GenericParameters);
                Assert.NotNull(methodInt.GenericParameters);

                Assert.NotEmpty(methodClass.GenericParameters);
                Assert.NotEmpty(methodInt.GenericParameters);
            }
            else
            {
                Assert.NotNull(methodClass.GenericParameters);
                Assert.NotNull(methodInt.GenericParameters);
                Assert.Empty(methodClass.GenericParameters);
                Assert.Empty(methodInt.GenericParameters);
            }
        }

        [Fact]
        public void LoadGetterSetterPropertyTest()
        {
            var type = typeof(ClassWithGetterSetterProperties);

            var assemblyPath = type.Assembly.Location;

            using var portableExecutableReader = new PEReader(File.OpenRead(assemblyPath));

            var metadataReader = portableExecutableReader.GetMetadataReader();
            var typeDefinitionHandle = GetTypeDefinitionHandle(type, metadataReader);

            var declarationFactory = DeclarationHelper.CreateDeclarationFactory(this.testOutputHelper);
            var declaration = declarationFactory.CreateClassDeclaration(metadataReader, typeDefinitionHandle, assemblyPath);

            var classDeclaration = Assert.IsType<ClassDeclaration>(declaration);

            var declarationResolverMock = new Mock<IDeclarationResolver>();
            classDeclaration.DeepLoad(declarationResolverMock.Object);

            Assert.NotEmpty(classDeclaration.Properties);
            Assert.Equal(3, classDeclaration.Properties.Count);

            var rwp = Assert.Single(classDeclaration.Properties.Where(p => p.Name == nameof(ClassWithGetterSetterProperties.ReadWriteProperty)));
            Assert.True(rwp.HasGetter);
            Assert.True(rwp.HasSetter);

            var rop = Assert.Single(classDeclaration.Properties.Where(p => p.Name == nameof(ClassWithGetterSetterProperties.ReadOnlyProperty)));
            Assert.True(rop.HasGetter);
            Assert.False(rop.HasSetter);

            var wop = Assert.Single(classDeclaration.Properties.Where(p => p.Name == nameof(ClassWithGetterSetterProperties.WriteOnlyProperty)));
            Assert.False(wop.HasGetter);
            Assert.True(wop.HasSetter);
        }

        [Theory]
        [InlineData(typeof(PatternAttributedClass), nameof(PatternAttribute))]
        [InlineData(typeof(RepeatAttributedClass), nameof(RepeatAttribute))]
        public void LoadClassAttributes(Type type, string attributeName)
        {
            var assemblyPath = type.Assembly.Location;

            using var portableExecutableReader = new PEReader(File.OpenRead(assemblyPath));

            var metadataReader = portableExecutableReader.GetMetadataReader();
            var typeDefinitionHandle = GetTypeDefinitionHandle(type, metadataReader);

            var declaration = DeclarationHelper.CreateDeclarationFactory(this.testOutputHelper)
                .CreateClassDeclaration(metadataReader, typeDefinitionHandle, assemblyPath);

            Assert.NotNull(declaration);
            Assert.Equal(
                MetadataGenericDeclarationLoader<SyntaxNode>.GetNameWithoutGeneric(metadataReader, typeDefinitionHandle),
                declaration.Name);

            var classDeclaration = Assert.IsType<ClassDeclaration>(declaration);

            var declarationResolverMock = new Mock<IDeclarationResolver>();
            classDeclaration.DeepLoad(declarationResolverMock.Object);

            Assert.NotNull(declaration.Attributes);
            var attribute = Assert.Single(declaration.Attributes);

            Assert.Equal(attributeName, attribute.Name);

            Assert.NotNull(attribute.SyntaxNodeProvider);

            var node = attribute.SyntaxNodeProvider.SyntaxNode;
            Assert.NotNull(node);

            var attrText = node.ToString();
            Assert.Contains(attributeName, attrText, StringComparison.OrdinalIgnoreCase);
        }

        private static TypeDefinitionHandle GetTypeDefinitionHandle(Type type, MetadataReader metadataReader)
        {
            return metadataReader.TypeDefinitions.Single(th => MetadataGenericDeclarationLoader<SyntaxNode>.GetFullName(
                MetadataGenericDeclarationLoader<SyntaxNode>.GetNamespace(metadataReader, th),
                MetadataGenericDeclarationLoader<SyntaxNode>.GetName(metadataReader, th)) == type.FullName);
        }
    }
}
