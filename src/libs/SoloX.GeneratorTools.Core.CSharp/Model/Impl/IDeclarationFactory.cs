﻿// ----------------------------------------------------------------------
// <copyright file="IDeclarationFactory.cs" company="Xavier Solau">
// Copyright © 2021 Xavier Solau.
// Licensed under the MIT license.
// See LICENSE file in the project root for full license information.
// </copyright>
// ----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SoloX.GeneratorTools.Core.CSharp.Model.Impl
{
    /// <summary>
    /// Declaration factory used to create declaration instances depending the parsing or the reflection load.
    /// </summary>
    public interface IDeclarationFactory
    {
        /// <summary>
        /// Create an interface declaration from a syntax node.
        /// </summary>
        /// <param name="nameSpaceDecl">The interface name space.</param>
        /// <param name="usingDirectives">The using directives.</param>
        /// <param name="node">The interface syntax node to load the declaration from.</param>
        /// <param name="location">Location of the declaration.</param>
        /// <returns>The created interface declaration object.</returns>
        IInterfaceDeclaration CreateInterfaceDeclaration(
            string nameSpaceDecl,
            IReadOnlyList<string> usingDirectives,
            InterfaceDeclarationSyntax node,
            string location);

        /// <summary>
        /// Create an interface declaration from a syntax node.
        /// </summary>
        /// <param name="type">The interface type to load the declaration from.</param>
        /// <returns>The created interface declaration object.</returns>
        IInterfaceDeclaration CreateInterfaceDeclaration(Type type);

        /// <summary>
        /// Create a interface declaration from a metadata type definition.
        /// </summary>
        /// <param name="metadataReader">Metadata reader.</param>
        /// <param name="typeDefinitionHandle">Type definition.</param>
        /// <param name="location">Location of the declaration.</param>
        /// <returns></returns>
        IInterfaceDeclaration CreateInterfaceDeclaration(MetadataReader metadataReader, TypeDefinitionHandle typeDefinitionHandle, string location);

        /// <summary>
        /// Create a class declaration from a syntax node.
        /// </summary>
        /// <param name="nameSpaceDecl">The class name space.</param>
        /// <param name="usingDirectives">The using directives.</param>
        /// <param name="node">The class syntax node to load the declaration from.</param>
        /// <param name="location">Location of the declaration.</param>
        /// <returns>The created class declaration object.</returns>
        IClassDeclaration CreateClassDeclaration(
            string nameSpaceDecl,
            IReadOnlyList<string> usingDirectives,
            ClassDeclarationSyntax node,
            string location);

        /// <summary>
        /// Create a class declaration from a syntax node.
        /// </summary>
        /// <param name="type">The class type to load the declaration from.</param>
        /// <returns>The created class declaration object.</returns>
        IClassDeclaration CreateClassDeclaration(Type type);

        /// <summary>
        /// Create a class declaration from a metadata type definition.
        /// </summary>
        /// <param name="metadataReader">Metadata reader.</param>
        /// <param name="typeDefinitionHandle">Type definition.</param>
        /// <param name="location">Location of the declaration.</param>
        /// <returns></returns>
        IClassDeclaration CreateClassDeclaration(MetadataReader metadataReader, TypeDefinitionHandle typeDefinitionHandle, string location);

        /// <summary>
        /// Create a struct declaration from a syntax node.
        /// </summary>
        /// <param name="nameSpaceDecl">The struct name space.</param>
        /// <param name="usingDirectives">The using directives.</param>
        /// <param name="node">The struct syntax node to load the declaration from.</param>
        /// <param name="location">Location of the declaration.</param>
        /// <returns>The created struct declaration object.</returns>
        IStructDeclaration CreateStructDeclaration(
            string nameSpaceDecl,
            IReadOnlyList<string> usingDirectives,
            StructDeclarationSyntax node,
            string location);

        /// <summary>
        /// Create an enum declaration from a syntax node.
        /// </summary>
        /// <param name="nameSpaceDecl">The enum name space.</param>
        /// <param name="usingDirectives">The using directives.</param>
        /// <param name="node">The enum syntax node to load the declaration from.</param>
        /// <param name="location">Location of the declaration.</param>
        /// <returns>The created enum declaration object.</returns>
        IEnumDeclaration CreateEnumDeclaration(
            string nameSpaceDecl,
            IReadOnlyList<string> usingDirectives,
            EnumDeclarationSyntax node,
            string location);
    }
}
