
//  Microsoft (R) .NET Framework IL Disassembler.  Version 4.6.1590.0
//  Copyright (c) Microsoft Corporation.  All rights reserved.


// ----- DOS Header:
// Magic:                      0x5a4d
// Bytes on last page:         0x0090
// Pages in file:              0x0003
// Relocations:                0x0000
// Size of header (paragraphs):0x0004
// Min extra paragraphs:       0x0000
// Max extra paragraphs:       0xffff
// Initial (relative) SS:      0x0000
// Initial SP:                 0x00b8
// Checksum:                   0x0000
// Initial IP:                 0x0000
// Initial (relative) CS:      0x0000
// File addr. of reloc table:  0x0040
// Overlay number:             0x0000
// OEM identifier:             0x0000
// OEM info:                   0x0000
// File addr. of COFF header:  0x0080
// ----- COFF/PE Headers:
// Signature:                  0x00004550
// ----- COFF Header:
// Machine:                    0x014c
// Number of sections:         0x0003
// Time-date stamp:            0x68532ac5
// Ptr to symbol table:        0x00000000
// Number of symbols:          0x00000000
// Size of optional header:    0x00e0
// Characteristics:            0x2022
// ----- PE Optional Header (32 bit):
// Magic:                          0x010b
// Major linker version:           0x30
// Minor linker version:           0x00
// Size of code:                   0x0000b200
// Size of init.data:              0x00000a00
// Size of uninit.data:            0x00000000
// Addr. of entry point:           0x0000d0ce
// Base of code:                   0x00002000
// Base of data:                   0x0000e000
// Image base:                     0x50160000
// Section alignment:              0x00002000
// File alignment:                 0x00000200
// Major OS version:               0x0004
// Minor OS version:               0x0000
// Major image version:            0x0000
// Minor image version:            0x0000
// Major subsystem version:        0x0006
// Minor subsystem version:        0x0000
// Size of image:                  0x00012000
// Size of headers:                0x00000200
// Checksum:                       0x0001b192
// Subsystem:                      0x0003
// DLL characteristics:            0x8560
// Size of stack reserve:          0x00100000
// Size of stack commit:           0x00001000
// Size of heap reserve:           0x00100000
// Size of heap commit:            0x00001000
// Loader flags:                   0x00000000
// Directories:                    0x00000010
// 0x00000000 [0x00000000] address [size] of Export Directory:          
// 0x0000d07c [0x0000004f] address [size] of Import Directory:          
// 0x0000e000 [0x000007a8] address [size] of Resource Directory:        
// 0x00000000 [0x00000000] address [size] of Exception Directory:       
// 0x0000be00 [0x000051c8] address [size] of Security Directory:        
// 0x00010000 [0x0000000c] address [size] of Base Relocation Table:     
// 0x0000cf44 [0x0000001c] address [size] of Debug Directory:           
// 0x00000000 [0x00000000] address [size] of Architecture Specific:     
// 0x00000000 [0x00000000] address [size] of Global Pointer:            
// 0x00000000 [0x00000000] address [size] of TLS Directory:             
// 0x00000000 [0x00000000] address [size] of Load Config Directory:     
// 0x00000000 [0x00000000] address [size] of Bound Import Directory:    
// 0x00002000 [0x00000008] address [size] of Import Address Table:      
// 0x00000000 [0x00000000] address [size] of Delay Load IAT:            
// 0x00002008 [0x00000048] address [size] of CLR Header:                


// Image sections:
//              .text
//              0x0000b0d4 Virtual Size
//              0x00002000 Virtual Address
//              0x0000b200 Size of Raw Data
//              0x00000200 Pointer to Raw Data
//              0x00000000 Pointer to Relocations
//              0x00000000 Pointer to Linenumbers
//                  0x0000 Number of Relocations
//                  0x0000 Number of Linenumbers
//              0x60000020 Characteristics
//                         CNT_CODE
//                         MEM_EXECUTE
//                         MEM_READ

//              .rsrc
//              0x000007a8 Virtual Size
//              0x0000e000 Virtual Address
//              0x00000800 Size of Raw Data
//              0x0000b400 Pointer to Raw Data
//              0x00000000 Pointer to Relocations
//              0x00000000 Pointer to Linenumbers
//                  0x0000 Number of Relocations
//                  0x0000 Number of Linenumbers
//              0x40000040 Characteristics
//                         CNT_INITIALIZED_DATA
//                         MEM_READ

//              .reloc
//              0x0000000c Virtual Size
//              0x00010000 Virtual Address
//              0x00000200 Size of Raw Data
//              0x0000bc00 Pointer to Raw Data
//              0x00000000 Pointer to Relocations
//              0x00000000 Pointer to Linenumbers
//                  0x0000 Number of Relocations
//                  0x0000 Number of Linenumbers
//              0x42000040 Characteristics
//                         CNT_INITIALIZED_DATA
//                         MEM_DISCARDABLE
//                         MEM_READ

// Base Relocation Table
//              0x0000d000 Page RVA
//              0x0000000c Block Size
//              0x00000002 Number of Entries
//              Entry 1: Type 0x3 Offset 0x000000d0
//              Entry 2: Type 0x0 Offset 0x00000000

// Import Address Table
//     DLL : mscoree.dll
//              0x00002000 Import Address Table
//              0x0000d0be Import Name Table
//              0          Time Date Stamp
//              0          Index of First Forwarder Reference
//
//              0x0000  _CorDllMain

// Delay Load Import Address Table
// No data.

// Entry point code:
//FF 25 00 20 16 50 


// ----- CLR Header:
// Header size:                        0x00000048
// Major runtime version:              0x0002
// Minor runtime version:              0x0005
// 0x00004268 [0x00008734] address [size] of Metadata Directory:        
// Flags:                              0x00000009
// Entry point token:                  0x00000000
// 0x0000c99c [0x000004a8] address [size] of Resources Directory:       
// 0x0000ce44 [0x00000100] address [size] of Strong Name Signature:     
// 0x00000000 [0x00000000] address [size] of CodeManager Table:         
// 0x00000000 [0x00000000] address [size] of VTableFixups Directory:    
// 0x00000000 [0x00000000] address [size] of Export Address Table:      
// 0x00000000 [0x00000000] address [size] of Precompile Header:         

// Metadata Header
//    Storage Signature:
//              0x424a5342 Signature
//                  0x0001 Major Version
//                  0x0001 Minor Version
//              0x00000000 Extra Data Offset
//              0x0000000c Version String Length
//              'v4.0.30319' Version String
//    Storage Header:
//                    0x00 Flags
//                  0x0005 Number of Streams
//    Stream 1:
//              0x0000006c Offset
//              0x000028d8 Size
//              '#~' Name
//    Stream 2:
//              0x00002944 Offset
//              0x00002b04 Size
//              '#Strings' Name
//    Stream 3:
//              0x00005448 Offset
//              0x0000089c Size
//              '#US' Name
//    Stream 4:
//              0x00005ce4 Offset
//              0x00000010 Size
//              '#GUID' Name
//    Stream 5:
//              0x00005cf4 Offset
//              0x00002a40 Size
//              '#Blob' Name

//    Metadata Stream Header:
//              0x00000000 Reserved
//                    0x02 Major
//                    0x00 Minor
//                    0x00 Heaps
//                    0x01 Rid
//      0x00000b091fa25f57 MaskValid
//      0x000016003301fa00 Sorted

// Code Manager Table:
//  default


// Export Address Table Jumps:
// No data.


// Metadata version: v4.0.30319
.module extern advapi32.dll
.module extern advapi32
.assembly extern mscorlib
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern System.Configuration
{
  .publickeytoken = (B0 3F 5F 7F 11 D5 0A 3A )                         // .?_....:
  .ver 4:0:0:0
}
.assembly extern System
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly extern System.ServiceModel.Internals
{
  .publickeytoken = (31 BF 38 56 AD 36 4E 35 )                         // 1.8V.6N5
  .ver 4:0:0:0
}
.assembly extern System.Xml
{
  .publickeytoken = (B7 7A 5C 56 19 34 E0 89 )                         // .z\V.4..
  .ver 4:0:0:0
}
.assembly SMDiagnostics
{
  .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilationRelaxationsAttribute::.ctor(int32) = ( 01 00 08 00 00 00 00 00 ) 

  // --- The following custom attribute is added automatically, do not uncomment -------
  //  .custom instance void [mscorlib]System.Diagnostics.DebuggableAttribute::.ctor(valuetype [mscorlib]System.Diagnostics.DebuggableAttribute/DebuggingModes) = ( 01 00 02 00 00 00 00 00 ) 

  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 54 69 6E 66 6F 63 61 72 64 2C 20 50 75   // ...Tinfocard, Pu
                                                                                                                 62 6C 69 63 4B 65 79 3D 30 30 32 34 30 30 30 30   // blicKey=00240000
                                                                                                                 30 34 38 30 30 30 30 30 39 34 30 30 30 30 30 30   // 0480000094000000
                                                                                                                 30 36 30 32 30 30 30 30 30 30 32 34 30 30 30 30   // 0602000000240000
                                                                                                                 35 32 35 33 34 31 33 31 30 30 30 34 30 30 30 30   // 5253413100040000
                                                                                                                 30 31 30 30 30 31 30 30 30 37 64 31 66 61 35 37   // 0100010007d1fa57
                                                                                                                 63 34 61 65 64 39 66 30 61 33 32 65 38 34 61 61   // c4aed9f0a32e84aa
                                                                                                                 30 66 61 65 66 64 30 64 65 39 65 38 66 64 36 61   // 0faefd0de9e8fd6a
                                                                                                                 65 63 38 66 38 37 66 62 30 33 37 36 36 63 38 33   // ec8f87fb03766c83
                                                                                                                 34 63 39 39 39 32 31 65 62 32 33 62 65 37 39 61   // 4c99921eb23be79a
                                                                                                                 64 39 64 35 64 63 63 31 64 64 39 61 64 32 33 36   // d9d5dcc1dd9ad236
                                                                                                                 31 33 32 31 30 32 39 30 30 62 37 32 33 63 66 39   // 132102900b723cf9
                                                                                                                 38 30 39 35 37 66 63 34 65 31 37 37 31 30 38 66   // 80957fc4e177108f
                                                                                                                 63 36 30 37 37 37 34 66 32 39 65 38 33 32 30 65   // c607774f29e8320e
                                                                                                                 39 32 65 61 30 35 65 63 65 34 65 38 32 31 63 30   // 92ea05ece4e821c0
                                                                                                                 61 35 65 66 65 38 66 31 36 34 35 63 34 63 30 63   // a5efe8f1645c4c0c
                                                                                                                 39 33 63 31 61 62 39 39 32 38 35 64 36 32 32 63   // 93c1ab99285d622c
                                                                                                                 61 61 36 35 32 63 31 64 66 61 64 36 33 64 37 34   // aa652c1dfad63d74
                                                                                                                 35 64 36 66 32 64 65 35 66 31 37 65 35 65 61 66   // 5d6f2de5f17e5eaf
                                                                                                                 30 66 63 34 39 36 33 64 32 36 31 63 38 61 31 32   // 0fc4963d261c8a12
                                                                                                                 34 33 36 35 31 38 32 30 36 64 63 30 39 33 33 34   // 436518206dc09334
                                                                                                                 34 64 35 61 64 32 39 33 00 00 )                   // 4d5ad293..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 5E 49 6E 64 69 67 6F 55 64 70 54 72 61   // ...^IndigoUdpTra
                                                                                                                 6E 73 70 6F 72 74 2C 20 50 75 62 6C 69 63 4B 65   // nsport, PublicKe
                                                                                                                 79 3D 30 30 32 34 30 30 30 30 30 34 38 30 30 30   // y=00240000048000
                                                                                                                 30 30 39 34 30 30 30 30 30 30 30 36 30 32 30 30   // 0094000000060200
                                                                                                                 30 30 30 30 32 34 30 30 30 30 35 32 35 33 34 31   // 0000240000525341
                                                                                                                 33 31 30 30 30 34 30 30 30 30 30 31 30 30 30 31   // 3100040000010001
                                                                                                                 30 30 30 37 64 31 66 61 35 37 63 34 61 65 64 39   // 0007d1fa57c4aed9
                                                                                                                 66 30 61 33 32 65 38 34 61 61 30 66 61 65 66 64   // f0a32e84aa0faefd
                                                                                                                 30 64 65 39 65 38 66 64 36 61 65 63 38 66 38 37   // 0de9e8fd6aec8f87
                                                                                                                 66 62 30 33 37 36 36 63 38 33 34 63 39 39 39 32   // fb03766c834c9992
                                                                                                                 31 65 62 32 33 62 65 37 39 61 64 39 64 35 64 63   // 1eb23be79ad9d5dc
                                                                                                                 63 31 64 64 39 61 64 32 33 36 31 33 32 31 30 32   // c1dd9ad236132102
                                                                                                                 39 30 30 62 37 32 33 63 66 39 38 30 39 35 37 66   // 900b723cf980957f
                                                                                                                 63 34 65 31 37 37 31 30 38 66 63 36 30 37 37 37   // c4e177108fc60777
                                                                                                                 34 66 32 39 65 38 33 32 30 65 39 32 65 61 30 35   // 4f29e8320e92ea05
                                                                                                                 65 63 65 34 65 38 32 31 63 30 61 35 65 66 65 38   // ece4e821c0a5efe8
                                                                                                                 66 31 36 34 35 63 34 63 30 63 39 33 63 31 61 62   // f1645c4c0c93c1ab
                                                                                                                 39 39 32 38 35 64 36 32 32 63 61 61 36 35 32 63   // 99285d622caa652c
                                                                                                                 31 64 66 61 64 36 33 64 37 34 35 64 36 66 32 64   // 1dfad63d745d6f2d
                                                                                                                 65 35 66 31 37 65 35 65 61 66 30 66 63 34 39 36   // e5f17e5eaf0fc496
                                                                                                                 33 64 32 36 31 63 38 61 31 32 34 33 36 35 31 38   // 3d261c8a12436518
                                                                                                                 32 30 36 64 63 30 39 33 33 34 34 64 35 61 64 32   // 206dc093344d5ad2
                                                                                                                 39 33 00 00 )                                     // 93..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 69 4D 69 63 72 6F 73 6F 66 74 2E 54 72   // ...iMicrosoft.Tr
                                                                                                                 61 6E 73 61 63 74 69 6F 6E 73 2E 42 72 69 64 67   // ansactions.Bridg
                                                                                                                 65 2C 20 50 75 62 6C 69 63 4B 65 79 3D 30 30 32   // e, PublicKey=002
                                                                                                                 34 30 30 30 30 30 34 38 30 30 30 30 30 39 34 30   // 4000004800000940
                                                                                                                 30 30 30 30 30 30 36 30 32 30 30 30 30 30 30 32   // 0000006020000002
                                                                                                                 34 30 30 30 30 35 32 35 33 34 31 33 31 30 30 30   // 4000052534131000
                                                                                                                 34 30 30 30 30 30 31 30 30 30 31 30 30 30 37 64   // 400000100010007d
                                                                                                                 31 66 61 35 37 63 34 61 65 64 39 66 30 61 33 32   // 1fa57c4aed9f0a32
                                                                                                                 65 38 34 61 61 30 66 61 65 66 64 30 64 65 39 65   // e84aa0faefd0de9e
                                                                                                                 38 66 64 36 61 65 63 38 66 38 37 66 62 30 33 37   // 8fd6aec8f87fb037
                                                                                                                 36 36 63 38 33 34 63 39 39 39 32 31 65 62 32 33   // 66c834c99921eb23
                                                                                                                 62 65 37 39 61 64 39 64 35 64 63 63 31 64 64 39   // be79ad9d5dcc1dd9
                                                                                                                 61 64 32 33 36 31 33 32 31 30 32 39 30 30 62 37   // ad236132102900b7
                                                                                                                 32 33 63 66 39 38 30 39 35 37 66 63 34 65 31 37   // 23cf980957fc4e17
                                                                                                                 37 31 30 38 66 63 36 30 37 37 37 34 66 32 39 65   // 7108fc607774f29e
                                                                                                                 38 33 32 30 65 39 32 65 61 30 35 65 63 65 34 65   // 8320e92ea05ece4e
                                                                                                                 38 32 31 63 30 61 35 65 66 65 38 66 31 36 34 35   // 821c0a5efe8f1645
                                                                                                                 63 34 63 30 63 39 33 63 31 61 62 39 39 32 38 35   // c4c0c93c1ab99285
                                                                                                                 64 36 32 32 63 61 61 36 35 32 63 31 64 66 61 64   // d622caa652c1dfad
                                                                                                                 36 33 64 37 34 35 64 36 66 32 64 65 35 66 31 37   // 63d745d6f2de5f17
                                                                                                                 65 35 65 61 66 30 66 63 34 39 36 33 64 32 36 31   // e5eaf0fc4963d261
                                                                                                                 63 38 61 31 32 34 33 36 35 31 38 32 30 36 64 63   // c8a12436518206dc
                                                                                                                 30 39 33 33 34 34 64 35 61 64 32 39 33 00 00 )    // 093344d5ad293..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 6D 4D 69 63 72 6F 73 6F 66 74 2E 54 72   // ...mMicrosoft.Tr
                                                                                                                 61 6E 73 61 63 74 69 6F 6E 73 2E 42 72 69 64 67   // ansactions.Bridg
                                                                                                                 65 2E 44 74 63 2C 20 50 75 62 6C 69 63 4B 65 79   // e.Dtc, PublicKey
                                                                                                                 3D 30 30 32 34 30 30 30 30 30 34 38 30 30 30 30   // =002400000480000
                                                                                                                 30 39 34 30 30 30 30 30 30 30 36 30 32 30 30 30   // 0940000000602000
                                                                                                                 30 30 30 32 34 30 30 30 30 35 32 35 33 34 31 33   // 0002400005253413
                                                                                                                 31 30 30 30 34 30 30 30 30 30 31 30 30 30 31 30   // 1000400000100010
                                                                                                                 30 30 37 64 31 66 61 35 37 63 34 61 65 64 39 66   // 007d1fa57c4aed9f
                                                                                                                 30 61 33 32 65 38 34 61 61 30 66 61 65 66 64 30   // 0a32e84aa0faefd0
                                                                                                                 64 65 39 65 38 66 64 36 61 65 63 38 66 38 37 66   // de9e8fd6aec8f87f
                                                                                                                 62 30 33 37 36 36 63 38 33 34 63 39 39 39 32 31   // b03766c834c99921
                                                                                                                 65 62 32 33 62 65 37 39 61 64 39 64 35 64 63 63   // eb23be79ad9d5dcc
                                                                                                                 31 64 64 39 61 64 32 33 36 31 33 32 31 30 32 39   // 1dd9ad2361321029
                                                                                                                 30 30 62 37 32 33 63 66 39 38 30 39 35 37 66 63   // 00b723cf980957fc
                                                                                                                 34 65 31 37 37 31 30 38 66 63 36 30 37 37 37 34   // 4e177108fc607774
                                                                                                                 66 32 39 65 38 33 32 30 65 39 32 65 61 30 35 65   // f29e8320e92ea05e
                                                                                                                 63 65 34 65 38 32 31 63 30 61 35 65 66 65 38 66   // ce4e821c0a5efe8f
                                                                                                                 31 36 34 35 63 34 63 30 63 39 33 63 31 61 62 39   // 1645c4c0c93c1ab9
                                                                                                                 39 32 38 35 64 36 32 32 63 61 61 36 35 32 63 31   // 9285d622caa652c1
                                                                                                                 64 66 61 64 36 33 64 37 34 35 64 36 66 32 64 65   // dfad63d745d6f2de
                                                                                                                 35 66 31 37 65 35 65 61 66 30 66 63 34 39 36 33   // 5f17e5eaf0fc4963
                                                                                                                 64 32 36 31 63 38 61 31 32 34 33 36 35 31 38 32   // d261c8a124365182
                                                                                                                 30 36 64 63 30 39 33 33 34 34 64 35 61 64 32 39   // 06dc093344d5ad29
                                                                                                                 33 00 00 )                                        // 3..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 4A 53 79 73 74 65 6D 2E 49 64 65 6E 74 69   // ..JSystem.Identi
                                                                                                                 74 79 4D 6F 64 65 6C 2E 53 65 6C 65 63 74 6F 72   // tyModel.Selector
                                                                                                                 73 2C 20 50 75 62 6C 69 63 4B 65 79 3D 30 30 30   // s, PublicKey=000
                                                                                                                 30 30 30 30 30 30 30 30 30 30 30 30 30 30 34 30   // 0000000000000040
                                                                                                                 30 30 30 30 30 30 30 30 30 30 30 30 30 00 00 )    // 0000000000000..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 59 53 79 73 74 65 6D 2E 49 4F 2E 4C 6F   // ...YSystem.IO.Lo
                                                                                                                 67 2C 20 50 75 62 6C 69 63 4B 65 79 3D 30 30 32   // g, PublicKey=002
                                                                                                                 34 30 30 30 30 30 34 38 30 30 30 30 30 39 34 30   // 4000004800000940
                                                                                                                 30 30 30 30 30 30 36 30 32 30 30 30 30 30 30 32   // 0000006020000002
                                                                                                                 34 30 30 30 30 35 32 35 33 34 31 33 31 30 30 30   // 4000052534131000
                                                                                                                 34 30 30 30 30 30 31 30 30 30 31 30 30 30 37 64   // 400000100010007d
                                                                                                                 31 66 61 35 37 63 34 61 65 64 39 66 30 61 33 32   // 1fa57c4aed9f0a32
                                                                                                                 65 38 34 61 61 30 66 61 65 66 64 30 64 65 39 65   // e84aa0faefd0de9e
                                                                                                                 38 66 64 36 61 65 63 38 66 38 37 66 62 30 33 37   // 8fd6aec8f87fb037
                                                                                                                 36 36 63 38 33 34 63 39 39 39 32 31 65 62 32 33   // 66c834c99921eb23
                                                                                                                 62 65 37 39 61 64 39 64 35 64 63 63 31 64 64 39   // be79ad9d5dcc1dd9
                                                                                                                 61 64 32 33 36 31 33 32 31 30 32 39 30 30 62 37   // ad236132102900b7
                                                                                                                 32 33 63 66 39 38 30 39 35 37 66 63 34 65 31 37   // 23cf980957fc4e17
                                                                                                                 37 31 30 38 66 63 36 30 37 37 37 34 66 32 39 65   // 7108fc607774f29e
                                                                                                                 38 33 32 30 65 39 32 65 61 30 35 65 63 65 34 65   // 8320e92ea05ece4e
                                                                                                                 38 32 31 63 30 61 35 65 66 65 38 66 31 36 34 35   // 821c0a5efe8f1645
                                                                                                                 63 34 63 30 63 39 33 63 31 61 62 39 39 32 38 35   // c4c0c93c1ab99285
                                                                                                                 64 36 32 32 63 61 61 36 35 32 63 31 64 66 61 64   // d622caa652c1dfad
                                                                                                                 36 33 64 37 34 35 64 36 66 32 64 65 35 66 31 37   // 63d745d6f2de5f17
                                                                                                                 65 35 65 61 66 30 66 63 34 39 36 33 64 32 36 31   // e5eaf0fc4963d261
                                                                                                                 63 38 61 31 32 34 33 36 35 31 38 32 30 36 64 63   // c8a12436518206dc
                                                                                                                 30 39 33 33 34 34 64 35 61 64 32 39 33 00 00 )    // 093344d5ad293..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 48 53 79 73 74 65 6D 2E 52 75 6E 74 69 6D   // ..HSystem.Runtim
                                                                                                                 65 2E 53 65 72 69 61 6C 69 7A 61 74 69 6F 6E 2C   // e.Serialization,
                                                                                                                 20 50 75 62 6C 69 63 4B 65 79 3D 30 30 30 30 30   //  PublicKey=00000
                                                                                                                 30 30 30 30 30 30 30 30 30 30 30 30 34 30 30 30   // 0000000000004000
                                                                                                                 30 30 30 30 30 30 30 30 30 30 30 00 00 )          // 00000000000..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 3F 53 79 73 74 65 6D 2E 53 65 72 76 69 63   // ..?System.Servic
                                                                                                                 65 4D 6F 64 65 6C 2C 20 50 75 62 6C 69 63 4B 65   // eModel, PublicKe
                                                                                                                 79 3D 30 30 30 30 30 30 30 30 30 30 30 30 30 30   // y=00000000000000
                                                                                                                 30 30 30 34 30 30 30 30 30 30 30 30 30 30 30 30   // 0004000000000000
                                                                                                                 30 30 00 00 )                                     // 00..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 6A 53 79 73 74 65 6D 2E 53 65 72 76 69   // ...jSystem.Servi
                                                                                                                 63 65 4D 6F 64 65 6C 2E 41 63 74 69 76 61 74 69   // ceModel.Activati
                                                                                                                 6F 6E 2C 20 50 75 62 6C 69 63 4B 65 79 3D 30 30   // on, PublicKey=00
                                                                                                                 32 34 30 30 30 30 30 34 38 30 30 30 30 30 39 34   // 2400000480000094
                                                                                                                 30 30 30 30 30 30 30 36 30 32 30 30 30 30 30 30   // 0000000602000000
                                                                                                                 32 34 30 30 30 30 35 32 35 33 34 31 33 31 30 30   // 2400005253413100
                                                                                                                 30 34 30 30 30 30 30 31 30 30 30 31 30 30 62 35   // 04000001000100b5
                                                                                                                 66 63 39 30 65 37 30 32 37 66 36 37 38 37 31 65   // fc90e7027f67871e
                                                                                                                 37 37 33 61 38 66 64 65 38 39 33 38 63 38 31 64   // 773a8fde8938c81d
                                                                                                                 64 34 30 32 62 61 36 35 62 39 32 30 31 64 36 30   // d402ba65b9201d60
                                                                                                                 35 39 33 65 39 36 63 34 39 32 36 35 31 65 38 38   // 593e96c492651e88
                                                                                                                 39 63 63 31 33 66 31 34 31 35 65 62 62 35 33 66   // 9cc13f1415ebb53f
                                                                                                                 61 63 31 31 33 31 61 65 30 62 64 33 33 33 63 35   // ac1131ae0bd333c5
                                                                                                                 65 65 36 30 32 31 36 37 32 64 39 37 31 38 65 61   // ee6021672d9718ea
                                                                                                                 33 31 61 38 61 65 62 64 30 64 61 30 30 37 32 66   // 31a8aebd0da0072f
                                                                                                                 32 35 64 38 37 64 62 61 36 66 63 39 30 66 66 64   // 25d87dba6fc90ffd
                                                                                                                 35 39 38 65 64 34 64 61 33 35 65 34 34 63 33 39   // 598ed4da35e44c39
                                                                                                                 38 63 34 35 34 33 30 37 65 38 65 33 33 62 38 34   // 8c454307e8e33b84
                                                                                                                 32 36 31 34 33 64 61 65 63 39 66 35 39 36 38 33   // 26143daec9f59683
                                                                                                                 36 66 39 37 63 38 66 37 34 37 35 30 65 35 39 37   // 6f97c8f74750e597
                                                                                                                 35 63 36 34 65 32 31 38 39 66 34 35 64 65 66 34   // 5c64e2189f45def4
                                                                                                                 36 62 32 61 32 62 31 32 34 37 61 64 63 33 36 35   // 6b2a2b1247adc365
                                                                                                                 32 62 66 35 63 33 30 38 30 35 35 64 61 39 00 00 ) // 2bf5c308055da9..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 66 53 79 73 74 65 6D 2E 53 65 72 76 69   // ...fSystem.Servi
                                                                                                                 63 65 4D 6F 64 65 6C 2E 46 72 69 65 6E 64 2C 20   // ceModel.Friend, 
                                                                                                                 50 75 62 6C 69 63 4B 65 79 3D 30 30 32 34 30 30   // PublicKey=002400
                                                                                                                 30 30 30 34 38 30 30 30 30 30 39 34 30 30 30 30   // 0004800000940000
                                                                                                                 30 30 30 36 30 32 30 30 30 30 30 30 32 34 30 30   // 0006020000002400
                                                                                                                 30 30 35 32 35 33 34 31 33 31 30 30 30 34 30 30   // 0052534131000400
                                                                                                                 30 30 30 31 30 30 30 31 30 30 30 37 64 31 66 61   // 000100010007d1fa
                                                                                                                 35 37 63 34 61 65 64 39 66 30 61 33 32 65 38 34   // 57c4aed9f0a32e84
                                                                                                                 61 61 30 66 61 65 66 64 30 64 65 39 65 38 66 64   // aa0faefd0de9e8fd
                                                                                                                 36 61 65 63 38 66 38 37 66 62 30 33 37 36 36 63   // 6aec8f87fb03766c
                                                                                                                 38 33 34 63 39 39 39 32 31 65 62 32 33 62 65 37   // 834c99921eb23be7
                                                                                                                 39 61 64 39 64 35 64 63 63 31 64 64 39 61 64 32   // 9ad9d5dcc1dd9ad2
                                                                                                                 33 36 31 33 32 31 30 32 39 30 30 62 37 32 33 63   // 36132102900b723c
                                                                                                                 66 39 38 30 39 35 37 66 63 34 65 31 37 37 31 30   // f980957fc4e17710
                                                                                                                 38 66 63 36 30 37 37 37 34 66 32 39 65 38 33 32   // 8fc607774f29e832
                                                                                                                 30 65 39 32 65 61 30 35 65 63 65 34 65 38 32 31   // 0e92ea05ece4e821
                                                                                                                 63 30 61 35 65 66 65 38 66 31 36 34 35 63 34 63   // c0a5efe8f1645c4c
                                                                                                                 30 63 39 33 63 31 61 62 39 39 32 38 35 64 36 32   // 0c93c1ab99285d62
                                                                                                                 32 63 61 61 36 35 32 63 31 64 66 61 64 36 33 64   // 2caa652c1dfad63d
                                                                                                                 37 34 35 64 36 66 32 64 65 35 66 31 37 65 35 65   // 745d6f2de5f17e5e
                                                                                                                 61 66 30 66 63 34 39 36 33 64 32 36 31 63 38 61   // af0fc4963d261c8a
                                                                                                                 31 32 34 33 36 35 31 38 32 30 36 64 63 30 39 33   // 12436518206dc093
                                                                                                                 33 34 34 64 35 61 64 32 39 33 00 00 )             // 344d5ad293..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 4A 53 79 73 74 65 6D 2E 53 65 72 76 69 63   // ..JSystem.Servic
                                                                                                                 65 4D 6F 64 65 6C 2E 57 61 73 48 6F 73 74 69 6E   // eModel.WasHostin
                                                                                                                 67 2C 20 50 75 62 6C 69 63 4B 65 79 3D 30 30 30   // g, PublicKey=000
                                                                                                                 30 30 30 30 30 30 30 30 30 30 30 30 30 30 34 30   // 0000000000000040
                                                                                                                 30 30 30 30 30 30 30 30 30 30 30 30 30 00 00 )    // 0000000000000..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 40 53 79 73 74 65 6D 2E 49 64 65 6E 74 69   // ..@System.Identi
                                                                                                                 74 79 4D 6F 64 65 6C 2C 20 50 75 62 6C 69 63 4B   // tyModel, PublicK
                                                                                                                 65 79 3D 30 30 30 30 30 30 30 30 30 30 30 30 30   // ey=0000000000000
                                                                                                                 30 30 30 30 34 30 30 30 30 30 30 30 30 30 30 30   // 0000400000000000
                                                                                                                 30 30 30 00 00 )                                  // 000..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 49 53 79 73 74 65 6D 2E 49 64 65 6E 74 69   // ..ISystem.Identi
                                                                                                                 74 79 4D 6F 64 65 6C 2E 53 65 72 76 69 63 65 73   // tyModel.Services
                                                                                                                 2C 20 50 75 62 6C 69 63 4B 65 79 3D 30 30 30 30   // , PublicKey=0000
                                                                                                                 30 30 30 30 30 30 30 30 30 30 30 30 30 34 30 30   // 0000000000000400
                                                                                                                 30 30 30 30 30 30 30 30 30 30 30 30 00 00 )       // 000000000000..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 55 53 4D 53 76 63 48 6F 73 74 2C 20 50   // ...USMSvcHost, P
                                                                                                                 75 62 6C 69 63 4B 65 79 3D 30 30 32 34 30 30 30   // ublicKey=0024000
                                                                                                                 30 30 34 38 30 30 30 30 30 39 34 30 30 30 30 30   // 0048000009400000
                                                                                                                 30 30 36 30 32 30 30 30 30 30 30 32 34 30 30 30   // 0060200000024000
                                                                                                                 30 35 32 35 33 34 31 33 31 30 30 30 34 30 30 30   // 0525341310004000
                                                                                                                 30 30 31 30 30 30 31 30 30 30 37 64 31 66 61 35   // 00100010007d1fa5
                                                                                                                 37 63 34 61 65 64 39 66 30 61 33 32 65 38 34 61   // 7c4aed9f0a32e84a
                                                                                                                 61 30 66 61 65 66 64 30 64 65 39 65 38 66 64 36   // a0faefd0de9e8fd6
                                                                                                                 61 65 63 38 66 38 37 66 62 30 33 37 36 36 63 38   // aec8f87fb03766c8
                                                                                                                 33 34 63 39 39 39 32 31 65 62 32 33 62 65 37 39   // 34c99921eb23be79
                                                                                                                 61 64 39 64 35 64 63 63 31 64 64 39 61 64 32 33   // ad9d5dcc1dd9ad23
                                                                                                                 36 31 33 32 31 30 32 39 30 30 62 37 32 33 63 66   // 6132102900b723cf
                                                                                                                 39 38 30 39 35 37 66 63 34 65 31 37 37 31 30 38   // 980957fc4e177108
                                                                                                                 66 63 36 30 37 37 37 34 66 32 39 65 38 33 32 30   // fc607774f29e8320
                                                                                                                 65 39 32 65 61 30 35 65 63 65 34 65 38 32 31 63   // e92ea05ece4e821c
                                                                                                                 30 61 35 65 66 65 38 66 31 36 34 35 63 34 63 30   // 0a5efe8f1645c4c0
                                                                                                                 63 39 33 63 31 61 62 39 39 32 38 35 64 36 32 32   // c93c1ab99285d622
                                                                                                                 63 61 61 36 35 32 63 31 64 66 61 64 36 33 64 37   // caa652c1dfad63d7
                                                                                                                 34 35 64 36 66 32 64 65 35 66 31 37 65 35 65 61   // 45d6f2de5f17e5ea
                                                                                                                 66 30 66 63 34 39 36 33 64 32 36 31 63 38 61 31   // f0fc4963d261c8a1
                                                                                                                 32 34 33 36 35 31 38 32 30 36 64 63 30 39 33 33   // 2436518206dc0933
                                                                                                                 34 34 64 35 61 64 32 39 33 00 00 )                // 44d5ad293..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 55 43 68 61 6E 6E 65 6C 46 78 2C 20 50   // ...UChannelFx, P
                                                                                                                 75 62 6C 69 63 4B 65 79 3D 30 30 32 34 30 30 30   // ublicKey=0024000
                                                                                                                 30 30 34 38 30 30 30 30 30 39 34 30 30 30 30 30   // 0048000009400000
                                                                                                                 30 30 36 30 32 30 30 30 30 30 30 32 34 30 30 30   // 0060200000024000
                                                                                                                 30 35 32 35 33 34 31 33 31 30 30 30 34 30 30 30   // 0525341310004000
                                                                                                                 30 30 31 30 30 30 31 30 30 30 37 64 31 66 61 35   // 00100010007d1fa5
                                                                                                                 37 63 34 61 65 64 39 66 30 61 33 32 65 38 34 61   // 7c4aed9f0a32e84a
                                                                                                                 61 30 66 61 65 66 64 30 64 65 39 65 38 66 64 36   // a0faefd0de9e8fd6
                                                                                                                 61 65 63 38 66 38 37 66 62 30 33 37 36 36 63 38   // aec8f87fb03766c8
                                                                                                                 33 34 63 39 39 39 32 31 65 62 32 33 62 65 37 39   // 34c99921eb23be79
                                                                                                                 61 64 39 64 35 64 63 63 31 64 64 39 61 64 32 33   // ad9d5dcc1dd9ad23
                                                                                                                 36 31 33 32 31 30 32 39 30 30 62 37 32 33 63 66   // 6132102900b723cf
                                                                                                                 39 38 30 39 35 37 66 63 34 65 31 37 37 31 30 38   // 980957fc4e177108
                                                                                                                 66 63 36 30 37 37 37 34 66 32 39 65 38 33 32 30   // fc607774f29e8320
                                                                                                                 65 39 32 65 61 30 35 65 63 65 34 65 38 32 31 63   // e92ea05ece4e821c
                                                                                                                 30 61 35 65 66 65 38 66 31 36 34 35 63 34 63 30   // 0a5efe8f1645c4c0
                                                                                                                 63 39 33 63 31 61 62 39 39 32 38 35 64 36 32 32   // c93c1ab99285d622
                                                                                                                 63 61 61 36 35 32 63 31 64 66 61 64 36 33 64 37   // caa652c1dfad63d7
                                                                                                                 34 35 64 36 66 32 64 65 35 66 31 37 65 35 65 61   // 45d6f2de5f17e5ea
                                                                                                                 66 30 66 63 34 39 36 33 64 32 36 31 63 38 61 31   // f0fc4963d261c8a1
                                                                                                                 32 34 33 36 35 31 38 32 30 36 64 63 30 39 33 33   // 2436518206dc0933
                                                                                                                 34 34 64 35 61 64 32 39 33 00 00 )                // 44d5ad293..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 63 53 79 73 74 65 6D 2E 57 6F 72 6B 66   // ...cSystem.Workf
                                                                                                                 6C 6F 77 53 65 72 76 69 63 65 73 2C 20 50 75 62   // lowServices, Pub
                                                                                                                 6C 69 63 4B 65 79 3D 30 30 32 34 30 30 30 30 30   // licKey=002400000
                                                                                                                 34 38 30 30 30 30 30 39 34 30 30 30 30 30 30 30   // 4800000940000000
                                                                                                                 36 30 32 30 30 30 30 30 30 32 34 30 30 30 30 35   // 6020000002400005
                                                                                                                 32 35 33 34 31 33 31 30 30 30 34 30 30 30 30 30   // 2534131000400000
                                                                                                                 31 30 30 30 31 30 30 62 35 66 63 39 30 65 37 30   // 1000100b5fc90e70
                                                                                                                 32 37 66 36 37 38 37 31 65 37 37 33 61 38 66 64   // 27f67871e773a8fd
                                                                                                                 65 38 39 33 38 63 38 31 64 64 34 30 32 62 61 36   // e8938c81dd402ba6
                                                                                                                 35 62 39 32 30 31 64 36 30 35 39 33 65 39 36 63   // 5b9201d60593e96c
                                                                                                                 34 39 32 36 35 31 65 38 38 39 63 63 31 33 66 31   // 492651e889cc13f1
                                                                                                                 34 31 35 65 62 62 35 33 66 61 63 31 31 33 31 61   // 415ebb53fac1131a
                                                                                                                 65 30 62 64 33 33 33 63 35 65 65 36 30 32 31 36   // e0bd333c5ee60216
                                                                                                                 37 32 64 39 37 31 38 65 61 33 31 61 38 61 65 62   // 72d9718ea31a8aeb
                                                                                                                 64 30 64 61 30 30 37 32 66 32 35 64 38 37 64 62   // d0da0072f25d87db
                                                                                                                 61 36 66 63 39 30 66 66 64 35 39 38 65 64 34 64   // a6fc90ffd598ed4d
                                                                                                                 61 33 35 65 34 34 63 33 39 38 63 34 35 34 33 30   // a35e44c398c45430
                                                                                                                 37 65 38 65 33 33 62 38 34 32 36 31 34 33 64 61   // 7e8e33b8426143da
                                                                                                                 65 63 39 66 35 39 36 38 33 36 66 39 37 63 38 66   // ec9f596836f97c8f
                                                                                                                 37 34 37 35 30 65 35 39 37 35 63 36 34 65 32 31   // 74750e5975c64e21
                                                                                                                 38 39 66 34 35 64 65 66 34 36 62 32 61 32 62 31   // 89f45def46b2a2b1
                                                                                                                 32 34 37 61 64 63 33 36 35 32 62 66 35 63 33 30   // 247adc3652bf5c30
                                                                                                                 38 30 35 35 64 61 39 00 00 )                      // 8055da9..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 63 53 79 73 74 65 6D 2E 53 65 72 76 69   // ...cSystem.Servi
                                                                                                                 63 65 4D 6F 64 65 6C 2E 57 65 62 2C 20 50 75 62   // ceModel.Web, Pub
                                                                                                                 6C 69 63 4B 65 79 3D 30 30 32 34 30 30 30 30 30   // licKey=002400000
                                                                                                                 34 38 30 30 30 30 30 39 34 30 30 30 30 30 30 30   // 4800000940000000
                                                                                                                 36 30 32 30 30 30 30 30 30 32 34 30 30 30 30 35   // 6020000002400005
                                                                                                                 32 35 33 34 31 33 31 30 30 30 34 30 30 30 30 30   // 2534131000400000
                                                                                                                 31 30 30 30 31 30 30 62 35 66 63 39 30 65 37 30   // 1000100b5fc90e70
                                                                                                                 32 37 66 36 37 38 37 31 65 37 37 33 61 38 66 64   // 27f67871e773a8fd
                                                                                                                 65 38 39 33 38 63 38 31 64 64 34 30 32 62 61 36   // e8938c81dd402ba6
                                                                                                                 35 62 39 32 30 31 64 36 30 35 39 33 65 39 36 63   // 5b9201d60593e96c
                                                                                                                 34 39 32 36 35 31 65 38 38 39 63 63 31 33 66 31   // 492651e889cc13f1
                                                                                                                 34 31 35 65 62 62 35 33 66 61 63 31 31 33 31 61   // 415ebb53fac1131a
                                                                                                                 65 30 62 64 33 33 33 63 35 65 65 36 30 32 31 36   // e0bd333c5ee60216
                                                                                                                 37 32 64 39 37 31 38 65 61 33 31 61 38 61 65 62   // 72d9718ea31a8aeb
                                                                                                                 64 30 64 61 30 30 37 32 66 32 35 64 38 37 64 62   // d0da0072f25d87db
                                                                                                                 61 36 66 63 39 30 66 66 64 35 39 38 65 64 34 64   // a6fc90ffd598ed4d
                                                                                                                 61 33 35 65 34 34 63 33 39 38 63 34 35 34 33 30   // a35e44c398c45430
                                                                                                                 37 65 38 65 33 33 62 38 34 32 36 31 34 33 64 61   // 7e8e33b8426143da
                                                                                                                 65 63 39 66 35 39 36 38 33 36 66 39 37 63 38 66   // ec9f596836f97c8f
                                                                                                                 37 34 37 35 30 65 35 39 37 35 63 36 34 65 32 31   // 74750e5975c64e21
                                                                                                                 38 39 66 34 35 64 65 66 34 36 62 32 61 32 62 31   // 89f45def46b2a2b1
                                                                                                                 32 34 37 61 64 63 33 36 35 32 62 66 35 63 33 30   // 247adc3652bf5c30
                                                                                                                 38 30 35 35 64 61 39 00 00 )                      // 8055da9..
  .custom instance void [mscorlib]System.Runtime.CompilerServices.InternalsVisibleToAttribute::.ctor(string) = ( 01 00 81 6B 4D 69 63 72 6F 73 6F 66 74 2E 53 65   // ...kMicrosoft.Se
                                                                                                                 72 76 69 63 65 4D 6F 64 65 6C 2E 57 65 62 2E 54   // rviceModel.Web.T
                                                                                                                 65 73 74 2C 20 50 75 62 6C 69 63 4B 65 79 3D 30   // est, PublicKey=0
                                                                                                                 30 32 34 30 30 30 30 30 34 38 30 30 30 30 30 39   // 0240000048000009
                                                                                                                 34 30 30 30 30 30 30 30 36 30 32 30 30 30 30 30   // 4000000060200000
                                                                                                                 30 32 34 30 30 30 30 35 32 35 33 34 31 33 31 30   // 0240000525341310
                                                                                                                 30 30 34 30 30 30 30 30 31 30 30 30 31 30 30 62   // 004000001000100b
                                                                                                                 35 66 63 39 30 65 37 30 32 37 66 36 37 38 37 31   // 5fc90e7027f67871
                                                                                                                 65 37 37 33 61 38 66 64 65 38 39 33 38 63 38 31   // e773a8fde8938c81
                                                                                                                 64 64 34 30 32 62 61 36 35 62 39 32 30 31 64 36   // dd402ba65b9201d6
                                                                                                                 30 35 39 33 65 39 36 63 34 39 32 36 35 31 65 38   // 0593e96c492651e8
                                                                                                                 38 39 63 63 31 33 66 31 34 31 35 65 62 62 35 33   // 89cc13f1415ebb53
                                                                                                                 66 61 63 31 31 33 31 61 65 30 62 64 33 33 33 63   // fac1131ae0bd333c
                                                                                                                 35 65 65 36 30 32 31 36 37 32 64 39 37 31 38 65   // 5ee6021672d9718e
                                                                                                                 61 33 31 61 38 61 65 62 64 30 64 61 30 30 37 32   // a31a8aebd0da0072
                                                                                                                 66 32 35 64 38 37 64 62 61 36 66 63 39 30 66 66   // f25d87dba6fc90ff
                                                                                                                 64 35 39 38 65 64 34 64 61 33 35 65 34 34 63 33   // d598ed4da35e44c3
                                                                                                                 39 38 63 34 35 34 33 30 37 65 38 65 33 33 62 38   // 98c454307e8e33b8
                                                                                                                 34 32 36 31 34 33 64 61 65 63 39 66 35 39 36 38   // 426143daec9f5968
                                                                                                                 33 36 66 39 37 63 38 66 37 34 37 35 30 65 35 39   // 36f97c8f74750e59
                                                                                                                 37 35 63 36 34 65 32 31 38 39 66 34 35 64 65 66   // 75c64e2189f45def
                                                                                                                 34 36 62 32 61 32 62 31 32 34 37 61 64 63 33 36   // 46b2a2b1247adc36
                                                                                                                 35 32 62 66 35 63 33 30 38 30 35 35 64 61 39 00   // 52bf5c308055da9.
                                                                                                                 00 ) 
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComVisibleAttribute::.ctor(bool) = ( 01 00 00 00 00 ) 
  .custom instance void [mscorlib]System.CLSCompliantAttribute::.ctor(bool) = ( 01 00 01 00 00 ) 
  .custom instance void [mscorlib]System.Security.AllowPartiallyTrustedCallersAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
  .custom instance void [mscorlib]System.Security.SecurityRulesAttribute::.ctor(valuetype [mscorlib]System.Security.SecurityRuleSet) = ( 01 00 01 01 00 54 02 1B 53 6B 69 70 56 65 72 69   // .....T..SkipVeri
                                                                                                                                         66 69 63 61 74 69 6F 6E 49 6E 46 75 6C 6C 54 72   // ficationInFullTr
                                                                                                                                         75 73 74 01 )                                     // ust.
  .custom instance void [mscorlib]System.Reflection.AssemblyTitleAttribute::.ctor(string) = ( 01 00 11 53 4D 44 69 61 67 6E 6F 73 74 69 63 73   // ...SMDiagnostics
                                                                                              2E 64 6C 6C 00 00 )                               // .dll..
  .custom instance void [mscorlib]System.Reflection.AssemblyDescriptionAttribute::.ctor(string) = ( 01 00 11 53 4D 44 69 61 67 6E 6F 73 74 69 63 73   // ...SMDiagnostics
                                                                                                    2E 64 6C 6C 00 00 )                               // .dll..
  .custom instance void [mscorlib]System.Reflection.AssemblyDefaultAliasAttribute::.ctor(string) = ( 01 00 11 53 4D 44 69 61 67 6E 6F 73 74 69 63 73   // ...SMDiagnostics
                                                                                                     2E 64 6C 6C 00 00 )                               // .dll..
  .custom instance void [mscorlib]System.Reflection.AssemblyCompanyAttribute::.ctor(string) = ( 01 00 15 4D 69 63 72 6F 73 6F 66 74 20 43 6F 72   // ...Microsoft Cor
                                                                                                70 6F 72 61 74 69 6F 6E 00 00 )                   // poration..
  .custom instance void [mscorlib]System.Reflection.AssemblyProductAttribute::.ctor(string) = ( 01 00 1A 4D 69 63 72 6F 73 6F 66 74 C2 AE 20 2E   // ...Microsoft.. .
                                                                                                4E 45 54 20 46 72 61 6D 65 77 6F 72 6B 00 00 )    // NET Framework..
  .custom instance void [mscorlib]System.Reflection.AssemblyCopyrightAttribute::.ctor(string) = ( 01 00 2F C2 A9 20 4D 69 63 72 6F 73 6F 66 74 20   // ../.. Microsoft 
                                                                                                  43 6F 72 70 6F 72 61 74 69 6F 6E 2E 20 20 41 6C   // Corporation.  Al
                                                                                                  6C 20 72 69 67 68 74 73 20 72 65 73 65 72 76 65   // l rights reserve
                                                                                                  64 2E 00 00 )                                     // d...
  .custom instance void [mscorlib]System.Reflection.AssemblyFileVersionAttribute::.ctor(string) = ( 01 00 0A 34 2E 38 2E 39 32 32 31 2E 30 00 00 )    // ...4.8.9221.0..
  .custom instance void [mscorlib]System.Reflection.AssemblyInformationalVersionAttribute::.ctor(string) = ( 01 00 0A 34 2E 38 2E 39 32 32 31 2E 30 00 00 )    // ...4.8.9221.0..
  .custom instance void [mscorlib]System.Resources.SatelliteContractVersionAttribute::.ctor(string) = ( 01 00 07 34 2E 30 2E 30 2E 30 00 00 )             // ...4.0.0.0..
  .custom instance void [mscorlib]System.Resources.NeutralResourcesLanguageAttribute::.ctor(string) = ( 01 00 05 65 6E 2D 55 53 00 00 )                   // ...en-US..
  .custom instance void [mscorlib]System.Reflection.AssemblyDelaySignAttribute::.ctor(bool) = ( 01 00 01 00 00 ) 
  .custom instance void [mscorlib]System.Reflection.AssemblyKeyFileAttribute::.ctor(string) = ( 01 00 24 66 3A 5C 64 64 5C 74 6F 6F 6C 73 5C 64   // ..$f:\dd\tools\d
                                                                                                65 76 64 69 76 5C 45 63 6D 61 50 75 62 6C 69 63   // evdiv\EcmaPublic
                                                                                                4B 65 79 2E 73 6E 6B 00 00 )                      // Key.snk..
  .custom instance void [mscorlib]System.Reflection.AssemblySignatureKeyAttribute::.ctor(string,
                                                                                         string) = ( 01 00 82 40 30 30 32 34 30 30 30 30 30 63 38 30   // ...@002400000c80
                                                                                                     30 30 30 30 31 34 30 31 30 30 30 30 30 36 30 32   // 0000140100000602
                                                                                                     30 30 30 30 30 30 32 34 30 30 30 30 35 32 35 33   // 0000002400005253
                                                                                                     34 31 33 31 30 30 30 38 30 30 30 30 30 31 30 30   // 4131000800000100
                                                                                                     30 31 30 30 36 31 33 33 39 39 61 66 66 31 38 65   // 0100613399aff18e
                                                                                                     66 31 61 32 63 32 35 31 34 61 32 37 33 61 34 32   // f1a2c2514a273a42
                                                                                                     64 39 30 34 32 62 37 32 33 32 31 66 31 37 35 37   // d9042b72321f1757
                                                                                                     31 30 32 64 66 39 65 62 61 64 61 36 39 39 32 33   // 102df9ebada69923
                                                                                                     65 32 37 33 38 34 30 36 63 32 31 65 35 62 38 30   // e2738406c21e5b80
                                                                                                     31 35 35 32 61 62 38 64 32 30 30 61 36 35 61 32   // 1552ab8d200a65a2
                                                                                                     33 35 65 30 30 31 61 63 39 61 64 63 32 35 66 32   // 35e001ac9adc25f2
                                                                                                     64 38 31 31 65 62 30 39 34 39 36 61 34 63 36 61   // d811eb09496a4c6a
                                                                                                     35 39 64 34 36 31 39 35 38 39 63 36 39 66 35 62   // 59d4619589c69f5b
                                                                                                     61 66 30 63 34 31 37 39 61 34 37 33 31 31 64 39   // af0c4179a47311d9
                                                                                                     32 35 35 35 63 64 30 30 36 61 63 63 38 62 35 39   // 2555cd006acc8b59
                                                                                                     35 39 66 32 62 64 36 65 31 30 65 33 36 30 63 33   // 59f2bd6e10e360c3
                                                                                                     34 35 33 37 61 31 64 32 36 36 64 61 38 30 38 35   // 4537a1d266da8085
                                                                                                     38 35 36 35 38 33 63 38 35 64 38 31 64 61 37 66   // 856583c85d81da7f
                                                                                                     33 65 63 30 31 65 64 39 35 36 34 63 35 38 64 39   // 3ec01ed9564c58d9
                                                                                                     33 64 37 31 33 63 64 30 31 37 32 63 38 65 32 33   // 3d713cd0172c8e23
                                                                                                     61 31 30 66 30 32 33 39 62 38 30 63 39 36 62 30   // a10f0239b80c96b0
                                                                                                     37 37 33 36 66 35 64 38 62 30 32 32 35 34 32 61   // 7736f5d8b022542a
                                                                                                     34 65 37 34 32 35 31 61 35 66 34 33 32 38 32 34   // 4e74251a5f432824
                                                                                                     33 31 38 62 33 35 33 39 61 35 61 30 38 37 66 38   // 318b3539a5a087f8
                                                                                                     65 35 33 64 32 66 31 33 35 66 39 63 61 34 37 66   // e53d2f135f9ca47f
                                                                                                     33 62 62 32 65 31 30 61 66 66 30 61 66 30 38 34   // 3bb2e10aff0af084
                                                                                                     39 35 30 34 66 62 37 63 65 61 33 66 66 31 39 32   // 9504fb7cea3ff192
                                                                                                     64 63 38 64 65 30 65 64 61 64 36 34 63 36 38 65   // dc8de0edad64c68e
                                                                                                     66 64 65 33 34 63 35 36 64 33 30 32 61 64 35 35   // fde34c56d302ad55
                                                                                                     66 64 36 65 38 30 66 33 30 32 64 35 65 66 63 64   // fd6e80f302d5efcd
                                                                                                     65 61 65 39 35 33 36 35 38 64 33 34 35 32 35 36   // eae953658d345256
                                                                                                     31 62 35 66 33 36 63 35 34 32 65 66 64 62 64 64   // 1b5f36c542efdbdd
                                                                                                     39 66 38 38 38 35 33 38 64 33 37 34 63 65 66 31   // 9f888538d374cef1
                                                                                                     30 36 61 63 66 37 64 39 33 61 34 34 34 35 63 33   // 06acf7d93a4445c3
                                                                                                     63 37 33 63 64 39 31 31 66 30 35 37 31 61 61 66   // c73cd911f0571aaf
                                                                                                     33 64 35 34 64 61 31 32 62 31 31 64 64 65 63 33   // 3d54da12b11ddec3
                                                                                                     37 35 62 33 81 00 61 35 61 38 36 36 65 31 65 65   // 75b3..a5a866e1ee
                                                                                                     31 38 36 66 38 30 37 36 36 38 32 30 39 66 33 62   // 186f807668209f3b
                                                                                                     31 31 32 33 36 61 63 65 35 65 32 31 66 31 31 37   // 11236ace5e21f117
                                                                                                     38 30 33 61 33 31 34 33 61 62 62 31 32 36 64 64   // 803a3143abb126dd
                                                                                                     30 33 35 64 37 64 32 66 38 37 36 62 36 39 33 38   // 035d7d2f876b6938
                                                                                                     61 61 66 32 65 65 33 34 31 34 64 35 34 32 30 64   // aaf2ee3414d5420d
                                                                                                     37 35 33 36 32 31 34 30 30 64 62 34 34 61 34 39   // 753621400db44a49
                                                                                                     63 34 38 36 63 65 31 33 34 33 30 30 61 32 31 30   // c486ce134300a210
                                                                                                     36 61 64 62 36 62 64 62 34 33 33 35 39 30 66 65   // 6adb6bdb433590fe
                                                                                                     66 38 61 64 35 63 34 33 63 62 61 38 32 32 39 30   // f8ad5c43cba82290
                                                                                                     64 63 34 39 35 33 30 65 66 66 64 38 36 35 32 33   // dc49530effd86523
                                                                                                     64 39 34 38 33 63 30 30 66 34 35 38 61 66 34 36   // d9483c00f458af46
                                                                                                     38 39 30 30 33 36 62 30 65 32 63 36 31 64 30 37   // 890036b0e2c61d07
                                                                                                     37 64 37 66 62 61 63 34 36 37 61 35 30 36 65 62   // 7d7fbac467a506eb
                                                                                                     61 32 39 65 34 36 37 61 38 37 31 39 38 62 30 35   // a29e467a87198b05
                                                                                                     33 63 37 34 39 61 61 32 61 34 64 32 38 34 30 63   // 3c749aa2a4d2840c
                                                                                                     37 38 34 65 36 64 00 00 )                         // 784e6d..
  .custom instance void [mscorlib]System.Runtime.InteropServices.ComCompatibleVersionAttribute::.ctor(int32,
                                                                                                      int32,
                                                                                                      int32,
                                                                                                      int32) = ( 01 00 01 00 00 00 00 00 00 00 E4 0C 00 00 00 00 
                                                                                                                 00 00 00 00 ) 
  .custom instance void [mscorlib]System.Runtime.CompilerServices.RuntimeCompatibilityAttribute::.ctor() = ( 01 00 01 00 54 02 16 57 72 61 70 4E 6F 6E 45 78   // ....T..WrapNonEx
                                                                                                             63 65 70 74 69 6F 6E 54 68 72 6F 77 73 01 )       // ceptionThrows.
  .custom instance void [mscorlib]System.Runtime.InteropServices.DefaultDllImportSearchPathsAttribute::.ctor(valuetype [mscorlib]System.Runtime.InteropServices.DllImportSearchPath) = ( 01 00 02 08 00 00 00 00 ) 
  .permissionset reqmin
             = {[mscorlib]System.Security.Permissions.SecurityPermissionAttribute = {property bool 'SkipVerification' = bool(true)}}
  .publickey = (00 00 00 00 00 00 00 00 04 00 00 00 00 00 00 00 ) 
  .hash algorithm 0x00008004
  .ver 4:0:0:0
}
.mresource public SMDiagnostics.resources
{
  // Offset: 0x00000000 Length: 0x000004A1
  // WARNING: managed resource file SMDiagnostics.resources created
}
.module SMDiagnostics.dll
// MVID: {F57B9B67-9EC5-4798-977A-8627CBC928F0}
.custom instance void [mscorlib]System.Security.UnverifiableCodeAttribute::.ctor() = ( 01 00 00 00 ) 
.imagebase 0x50160000
.file alignment 0x00000200
.stackreserve 0x00100000
.subsystem 0x0003       // WINDOWS_CUI
.corflags 0x00000009    //  ILONLY
// Image base: 0x000001982E8D0000


// =============== CLASS MEMBERS DECLARATION ===================

.class private auto ansi beforefieldinit System.ServiceModel.Configuration.MachineSettingsSection
       extends [System.Configuration]System.Configuration.ConfigurationSection
{
  .field private static bool enableLoggingKnownPii
  .field private static bool hasInitialized
  .field private static object syncRoot
  .field private static literal string enableLoggingKnownPiiKey = "enableLoggingKnownPii"
  .field private class [System.Configuration]System.Configuration.ConfigurationPropertyCollection properties
  .method family hidebysig specialname virtual 
          instance class [System.Configuration]System.Configuration.ConfigurationPropertyCollection 
          get_Properties() cil managed
  {
    // Code size       63 (0x3f)
    .maxstack  7
    .locals init (class [System.Configuration]System.Configuration.ConfigurationPropertyCollection V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [System.Configuration]System.Configuration.ConfigurationPropertyCollection System.ServiceModel.Configuration.MachineSettingsSection::properties
    IL_0006:  brtrue.s   IL_0038

    IL_0008:  newobj     instance void [System.Configuration]System.Configuration.ConfigurationPropertyCollection::.ctor()
    IL_000d:  stloc.0
    IL_000e:  ldloc.0
    IL_000f:  ldstr      "enableLoggingKnownPii"
    IL_0014:  ldtoken    [mscorlib]System.Boolean
    IL_0019:  call       class [mscorlib]System.Type [mscorlib]System.Type::GetTypeFromHandle(valuetype [mscorlib]System.RuntimeTypeHandle)
    IL_001e:  ldc.i4.0
    IL_001f:  box        [mscorlib]System.Boolean
    IL_0024:  ldnull
    IL_0025:  ldnull
    IL_0026:  ldc.i4.0
    IL_0027:  newobj     instance void [System.Configuration]System.Configuration.ConfigurationProperty::.ctor(string,
                                                                                                               class [mscorlib]System.Type,
                                                                                                               object,
                                                                                                               class [System]System.ComponentModel.TypeConverter,
                                                                                                               class [System.Configuration]System.Configuration.ConfigurationValidatorBase,
                                                                                                               valuetype [System.Configuration]System.Configuration.ConfigurationPropertyOptions)
    IL_002c:  callvirt   instance void [System.Configuration]System.Configuration.ConfigurationPropertyCollection::Add(class [System.Configuration]System.Configuration.ConfigurationProperty)
    IL_0031:  ldarg.0
    IL_0032:  ldloc.0
    IL_0033:  stfld      class [System.Configuration]System.Configuration.ConfigurationPropertyCollection System.ServiceModel.Configuration.MachineSettingsSection::properties
    IL_0038:  ldarg.0
    IL_0039:  ldfld      class [System.Configuration]System.Configuration.ConfigurationPropertyCollection System.ServiceModel.Configuration.MachineSettingsSection::properties
    IL_003e:  ret
  } // end of method MachineSettingsSection::get_Properties

  .method public hidebysig specialname static 
          bool  get_EnableLoggingKnownPii() cil managed
  {
    // Code size       91 (0x5b)
    .maxstack  2
    .locals init (object V_0,
             bool V_1,
             class System.ServiceModel.Configuration.MachineSettingsSection V_2)
    IL_0000:  ldsfld     bool System.ServiceModel.Configuration.MachineSettingsSection::hasInitialized
    IL_0005:  brtrue.s   IL_0055

    IL_0007:  ldsfld     object System.ServiceModel.Configuration.MachineSettingsSection::syncRoot
    IL_000c:  stloc.0
    IL_000d:  ldc.i4.0
    IL_000e:  stloc.1
    .try
    {
      IL_000f:  ldloc.0
      IL_0010:  ldloca.s   V_1
      IL_0012:  call       void [mscorlib]System.Threading.Monitor::Enter(object,
                                                                          bool&)
      IL_0017:  ldsfld     bool System.ServiceModel.Configuration.MachineSettingsSection::hasInitialized
      IL_001c:  brtrue.s   IL_0049

      IL_001e:  ldstr      "system.serviceModel/machineSettings"
      IL_0023:  call       object [System.Configuration]System.Configuration.ConfigurationManager::GetSection(string)
      IL_0028:  castclass  System.ServiceModel.Configuration.MachineSettingsSection
      IL_002d:  stloc.2
      IL_002e:  ldloc.2
      IL_002f:  ldstr      "enableLoggingKnownPii"
      IL_0034:  callvirt   instance object [System.Configuration]System.Configuration.ConfigurationElement::get_Item(string)
      IL_0039:  unbox.any  [mscorlib]System.Boolean
      IL_003e:  stsfld     bool System.ServiceModel.Configuration.MachineSettingsSection::enableLoggingKnownPii
      IL_0043:  ldc.i4.1
      IL_0044:  stsfld     bool System.ServiceModel.Configuration.MachineSettingsSection::hasInitialized
      IL_0049:  leave.s    IL_0055

    }  // end .try
    finally
    {
      IL_004b:  ldloc.1
      IL_004c:  brfalse.s  IL_0054

      IL_004e:  ldloc.0
      IL_004f:  call       void [mscorlib]System.Threading.Monitor::Exit(object)
      IL_0054:  endfinally
    }  // end handler
    IL_0055:  ldsfld     bool System.ServiceModel.Configuration.MachineSettingsSection::enableLoggingKnownPii
    IL_005a:  ret
  } // end of method MachineSettingsSection::get_EnableLoggingKnownPii

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [System.Configuration]System.Configuration.ConfigurationSection::.ctor()
    IL_0006:  ret
  } // end of method MachineSettingsSection::.ctor

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     bool System.ServiceModel.Configuration.MachineSettingsSection::hasInitialized
    IL_0006:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_000b:  stsfld     object System.ServiceModel.Configuration.MachineSettingsSection::syncRoot
    IL_0010:  ret
  } // end of method MachineSettingsSection::.cctor

  .property instance class [System.Configuration]System.Configuration.ConfigurationPropertyCollection
          Properties()
  {
    .get instance class [System.Configuration]System.Configuration.ConfigurationPropertyCollection System.ServiceModel.Configuration.MachineSettingsSection::get_Properties()
  } // end of property MachineSettingsSection::Properties
  .property bool EnableLoggingKnownPii()
  {
    .get bool System.ServiceModel.Configuration.MachineSettingsSection::get_EnableLoggingKnownPii()
  } // end of property MachineSettingsSection::EnableLoggingKnownPii
} // end of class System.ServiceModel.Configuration.MachineSettingsSection

.class private auto ansi beforefieldinit System.ServiceModel.Diagnostics.Activity
       extends [mscorlib]System.Object
       implements [mscorlib]System.IDisposable
{
  .field family valuetype [mscorlib]System.Guid parentId
  .field private valuetype [mscorlib]System.Guid currentId
  .field private bool mustDispose
  .method family hidebysig specialname rtspecialname 
          instance void  .ctor(valuetype [mscorlib]System.Guid activityId,
                               valuetype [mscorlib]System.Guid parentId) cil managed
  {
    // Code size       39 (0x27)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      valuetype [mscorlib]System.Guid System.ServiceModel.Diagnostics.Activity::currentId
    IL_000d:  ldarg.0
    IL_000e:  ldarg.2
    IL_000f:  stfld      valuetype [mscorlib]System.Guid System.ServiceModel.Diagnostics.Activity::parentId
    IL_0014:  ldarg.0
    IL_0015:  ldc.i4.1
    IL_0016:  stfld      bool System.ServiceModel.Diagnostics.Activity::mustDispose
    IL_001b:  ldarg.0
    IL_001c:  ldfld      valuetype [mscorlib]System.Guid System.ServiceModel.Diagnostics.Activity::currentId
    IL_0021:  call       void [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::set_ActivityId(valuetype [mscorlib]System.Guid)
    IL_0026:  ret
  } // end of method Activity::.ctor

  .method assembly hidebysig static class System.ServiceModel.Diagnostics.Activity 
          CreateActivity(valuetype [mscorlib]System.Guid activityId) cil managed
  {
    // Code size       40 (0x28)
    .maxstack  2
    .locals init (class System.ServiceModel.Diagnostics.Activity V_0,
             valuetype [mscorlib]System.Guid V_1)
    IL_0000:  ldnull
    IL_0001:  stloc.0
    IL_0002:  ldarg.0
    IL_0003:  ldsfld     valuetype [mscorlib]System.Guid [mscorlib]System.Guid::Empty
    IL_0008:  call       bool [mscorlib]System.Guid::op_Inequality(valuetype [mscorlib]System.Guid,
                                                                   valuetype [mscorlib]System.Guid)
    IL_000d:  brfalse.s  IL_0026

    IL_000f:  call       valuetype [mscorlib]System.Guid [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_ActivityId()
    IL_0014:  stloc.1
    IL_0015:  ldarg.0
    IL_0016:  ldloc.1
    IL_0017:  call       bool [mscorlib]System.Guid::op_Inequality(valuetype [mscorlib]System.Guid,
                                                                   valuetype [mscorlib]System.Guid)
    IL_001c:  brfalse.s  IL_0026

    IL_001e:  ldarg.0
    IL_001f:  ldloc.1
    IL_0020:  newobj     instance void System.ServiceModel.Diagnostics.Activity::.ctor(valuetype [mscorlib]System.Guid,
                                                                                       valuetype [mscorlib]System.Guid)
    IL_0025:  stloc.0
    IL_0026:  ldloc.0
    IL_0027:  ret
  } // end of method Activity::CreateActivity

  .method public hidebysig newslot virtual 
          instance void  Dispose() cil managed
  {
    // Code size       33 (0x21)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.Activity::mustDispose
    IL_0006:  brfalse.s  IL_001a

    IL_0008:  ldarg.0
    IL_0009:  ldc.i4.0
    IL_000a:  stfld      bool System.ServiceModel.Diagnostics.Activity::mustDispose
    IL_000f:  ldarg.0
    IL_0010:  ldfld      valuetype [mscorlib]System.Guid System.ServiceModel.Diagnostics.Activity::parentId
    IL_0015:  call       void [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::set_ActivityId(valuetype [mscorlib]System.Guid)
    IL_001a:  ldarg.0
    IL_001b:  call       void [mscorlib]System.GC::SuppressFinalize(object)
    IL_0020:  ret
  } // end of method Activity::Dispose

  .method family hidebysig specialname instance valuetype [mscorlib]System.Guid 
          get_Id() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      valuetype [mscorlib]System.Guid System.ServiceModel.Diagnostics.Activity::currentId
    IL_0006:  ret
  } // end of method Activity::get_Id

  .property instance valuetype [mscorlib]System.Guid
          Id()
  {
    .get instance valuetype [mscorlib]System.Guid System.ServiceModel.Diagnostics.Activity::get_Id()
  } // end of property Activity::Id
} // end of class System.ServiceModel.Diagnostics.Activity

.class private auto ansi beforefieldinit System.ServiceModel.Diagnostics.DiagnosticTraceSource
       extends System.ServiceModel.Diagnostics.PiiTraceSource
{
  .field private static literal string PropagateActivityValue = "propagateActivity"
  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(string name,
                               string eventSourceName) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  call       instance void System.ServiceModel.Diagnostics.PiiTraceSource::.ctor(string,
                                                                                             string)
    IL_0008:  ret
  } // end of method DiagnosticTraceSource::.ctor

  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(string name,
                               string eventSourceName,
                               valuetype [System]System.Diagnostics.SourceLevels level) cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  ldarg.3
    IL_0004:  call       instance void System.ServiceModel.Diagnostics.PiiTraceSource::.ctor(string,
                                                                                             string,
                                                                                             valuetype [System]System.Diagnostics.SourceLevels)
    IL_0009:  ret
  } // end of method DiagnosticTraceSource::.ctor

  .method family hidebysig virtual instance string[] 
          GetSupportedAttributes() cil managed
  {
    // Code size       50 (0x32)
    .maxstack  4
    .locals init (string[] V_0,
             string[] V_1,
             int32 V_2)
    IL_0000:  ldarg.0
    IL_0001:  call       instance string[] System.ServiceModel.Diagnostics.PiiTraceSource::GetSupportedAttributes()
    IL_0006:  stloc.0
    IL_0007:  ldloc.0
    IL_0008:  ldlen
    IL_0009:  conv.i4
    IL_000a:  ldc.i4.1
    IL_000b:  add
    IL_000c:  newarr     [mscorlib]System.String
    IL_0011:  stloc.1
    IL_0012:  ldc.i4.0
    IL_0013:  stloc.2
    IL_0014:  br.s       IL_0020

    IL_0016:  ldloc.1
    IL_0017:  ldloc.2
    IL_0018:  ldloc.0
    IL_0019:  ldloc.2
    IL_001a:  ldelem.ref
    IL_001b:  stelem.ref
    IL_001c:  ldloc.2
    IL_001d:  ldc.i4.1
    IL_001e:  add
    IL_001f:  stloc.2
    IL_0020:  ldloc.2
    IL_0021:  ldloc.0
    IL_0022:  ldlen
    IL_0023:  conv.i4
    IL_0024:  blt.s      IL_0016

    IL_0026:  ldloc.1
    IL_0027:  ldloc.0
    IL_0028:  ldlen
    IL_0029:  conv.i4
    IL_002a:  ldstr      "propagateActivity"
    IL_002f:  stelem.ref
    IL_0030:  ldloc.1
    IL_0031:  ret
  } // end of method DiagnosticTraceSource::GetSupportedAttributes

  .method assembly hidebysig specialname 
          instance bool  get_PropagateActivity() cil managed
  {
    // Code size       41 (0x29)
    .maxstack  2
    .locals init (bool V_0,
             string V_1)
    IL_0000:  ldc.i4.0
    IL_0001:  stloc.0
    IL_0002:  ldarg.0
    IL_0003:  call       instance class [System]System.Collections.Specialized.StringDictionary [System]System.Diagnostics.TraceSource::get_Attributes()
    IL_0008:  ldstr      "propagateActivity"
    IL_000d:  callvirt   instance string [System]System.Collections.Specialized.StringDictionary::get_Item(string)
    IL_0012:  stloc.1
    IL_0013:  ldloc.1
    IL_0014:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
    IL_0019:  brtrue.s   IL_0027

    IL_001b:  ldloc.1
    IL_001c:  ldloca.s   V_0
    IL_001e:  call       bool [mscorlib]System.Boolean::TryParse(string,
                                                                 bool&)
    IL_0023:  brtrue.s   IL_0027

    IL_0025:  ldc.i4.0
    IL_0026:  stloc.0
    IL_0027:  ldloc.0
    IL_0028:  ret
  } // end of method DiagnosticTraceSource::get_PropagateActivity

  .method assembly hidebysig specialname 
          instance void  set_PropagateActivity(bool 'value') cil managed
  {
    // Code size       24 (0x18)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance class [System]System.Collections.Specialized.StringDictionary [System]System.Diagnostics.TraceSource::get_Attributes()
    IL_0006:  ldstr      "propagateActivity"
    IL_000b:  ldarga.s   'value'
    IL_000d:  call       instance string [mscorlib]System.Boolean::ToString()
    IL_0012:  callvirt   instance void [System]System.Collections.Specialized.StringDictionary::set_Item(string,
                                                                                                         string)
    IL_0017:  ret
  } // end of method DiagnosticTraceSource::set_PropagateActivity

  .property instance bool PropagateActivity()
  {
    .get instance bool System.ServiceModel.Diagnostics.DiagnosticTraceSource::get_PropagateActivity()
    .set instance void System.ServiceModel.Diagnostics.DiagnosticTraceSource::set_PropagateActivity(bool)
  } // end of property DiagnosticTraceSource::PropagateActivity
} // end of class System.ServiceModel.Diagnostics.DiagnosticTraceSource

.class private abstract auto ansi sealed beforefieldinit System.ServiceModel.Diagnostics.DiagnosticStrings
       extends [mscorlib]System.Object
{
  .field static assembly literal string DiagnosticsNamespace = "http://schemas.microsoft.com/2004/09/ServiceModel/"
  + "Diagnostics"
  .field static assembly literal string ActivityIdName = "E2ETrace.ActivityID"
  .field static assembly literal string ActivityId = "ActivityId"
  .field static assembly literal string AppDomain = "AppDomain"
  .field static assembly literal string DataTag = "Data"
  .field static assembly literal string DataItemsTag = "DataItems"
  .field static assembly literal string DeflateCookieAfterDeflatingTag = "AfterDeflating"
  .field static assembly literal string DeflateCookieOriginalSizeTag = "OriginalSize"
  .field static assembly literal string DescriptionTag = "Description"
  .field static assembly literal string EventLogTag = "EventLog"
  .field static assembly literal string ExceptionTag = "Exception"
  .field static assembly literal string ExceptionTypeTag = "ExceptionType"
  .field static assembly literal string ExceptionStringTag = "ExceptionString"
  .field static assembly literal string ExtendedDataTag = "ExtendedData"
  .field static assembly literal string HeaderTag = "Header"
  .field static assembly literal string InnerExceptionTag = "InnerException"
  .field static assembly literal string KeyTag = "Key"
  .field static assembly literal string MessageTag = "Message"
  .field static assembly literal string NameTag = "Name"
  .field static assembly literal string NamespaceTag = "xmlns"
  .field static assembly literal string NativeErrorCodeTag = "NativeErrorCode"
  .field static assembly literal string ProcessId = "ProcessId"
  .field static assembly literal string ProcessName = "ProcessName"
  .field static assembly literal string RoleTag = "Role"
  .field static assembly literal string SeverityTag = "Severity"
  .field static assembly literal string SourceTag = "Source"
  .field static assembly literal string StackTraceTag = "StackTrace"
  .field static assembly literal string TraceCodeTag = "TraceIdentifier"
  .field static assembly literal string TraceRecordTag = "TraceRecord"
  .field static assembly literal string ValueTag = "Value"
  .field static assembly string[][] HeadersPaths
  .field static assembly string[] PiiList
  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       178 (0xb2)
    .maxstack  7
    IL_0000:  ldc.i4.2
    IL_0001:  newarr     string[]
    IL_0006:  dup
    IL_0007:  ldc.i4.0
    IL_0008:  ldc.i4.4
    IL_0009:  newarr     [mscorlib]System.String
    IL_000e:  dup
    IL_000f:  ldc.i4.0
    IL_0010:  ldstr      "TraceRecord"
    IL_0015:  stelem.ref
    IL_0016:  dup
    IL_0017:  ldc.i4.1
    IL_0018:  ldstr      "ExtendedData"
    IL_001d:  stelem.ref
    IL_001e:  dup
    IL_001f:  ldc.i4.2
    IL_0020:  ldstr      "MessageHeaders"
    IL_0025:  stelem.ref
    IL_0026:  dup
    IL_0027:  ldc.i4.3
    IL_0028:  ldstr      "Security"
    IL_002d:  stelem.ref
    IL_002e:  stelem.ref
    IL_002f:  dup
    IL_0030:  ldc.i4.1
    IL_0031:  ldc.i4.4
    IL_0032:  newarr     [mscorlib]System.String
    IL_0037:  dup
    IL_0038:  ldc.i4.0
    IL_0039:  ldstr      "TraceRecord"
    IL_003e:  stelem.ref
    IL_003f:  dup
    IL_0040:  ldc.i4.1
    IL_0041:  ldstr      "ExtendedData"
    IL_0046:  stelem.ref
    IL_0047:  dup
    IL_0048:  ldc.i4.2
    IL_0049:  ldstr      "MessageHeaders"
    IL_004e:  stelem.ref
    IL_004f:  dup
    IL_0050:  ldc.i4.3
    IL_0051:  ldstr      "IssuedTokens"
    IL_0056:  stelem.ref
    IL_0057:  stelem.ref
    IL_0058:  stsfld     string[][] System.ServiceModel.Diagnostics.DiagnosticStrings::HeadersPaths
    IL_005d:  ldc.i4.s   9
    IL_005f:  newarr     [mscorlib]System.String
    IL_0064:  dup
    IL_0065:  ldc.i4.0
    IL_0066:  ldstr      "BinarySecret"
    IL_006b:  stelem.ref
    IL_006c:  dup
    IL_006d:  ldc.i4.1
    IL_006e:  ldstr      "Entropy"
    IL_0073:  stelem.ref
    IL_0074:  dup
    IL_0075:  ldc.i4.2
    IL_0076:  ldstr      "Password"
    IL_007b:  stelem.ref
    IL_007c:  dup
    IL_007d:  ldc.i4.3
    IL_007e:  ldstr      "Nonce"
    IL_0083:  stelem.ref
    IL_0084:  dup
    IL_0085:  ldc.i4.4
    IL_0086:  ldstr      "Username"
    IL_008b:  stelem.ref
    IL_008c:  dup
    IL_008d:  ldc.i4.5
    IL_008e:  ldstr      "BinarySecurityToken"
    IL_0093:  stelem.ref
    IL_0094:  dup
    IL_0095:  ldc.i4.6
    IL_0096:  ldstr      "NameIdentifier"
    IL_009b:  stelem.ref
    IL_009c:  dup
    IL_009d:  ldc.i4.7
    IL_009e:  ldstr      "SubjectLocality"
    IL_00a3:  stelem.ref
    IL_00a4:  dup
    IL_00a5:  ldc.i4.8
    IL_00a6:  ldstr      "AttributeValue"
    IL_00ab:  stelem.ref
    IL_00ac:  stsfld     string[] System.ServiceModel.Diagnostics.DiagnosticStrings::PiiList
    IL_00b1:  ret
  } // end of method DiagnosticStrings::.cctor

} // end of class System.ServiceModel.Diagnostics.DiagnosticStrings

.class private auto ansi sealed System.ServiceModel.Diagnostics.EventLogCategory
       extends [mscorlib]System.Enum
{
  .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 45 54 68 69 73 20 68 61 73 20 62 65 65 6E   // ..EThis has been
                                                                              20 72 65 70 6C 61 63 65 64 20 62 79 20 53 79 73   //  replaced by Sys
                                                                              74 65 6D 2E 52 75 6E 74 69 6D 65 2E 44 69 61 67   // tem.Runtime.Diag
                                                                              6E 6F 73 74 69 63 73 2E 45 76 65 6E 74 4C 6F 67   // nostics.EventLog
                                                                              43 61 74 65 67 6F 72 79 00 00 )                   // Category..
  .field public specialname rtspecialname uint16 value__
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory ServiceAuthorization = uint16(0x0001)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory MessageAuthentication = uint16(0x0002)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory ObjectAccess = uint16(0x0003)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory Tracing = uint16(0x0004)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory WebHost = uint16(0x0005)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory FailFast = uint16(0x0006)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory MessageLogging = uint16(0x0007)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory PerformanceCounter = uint16(0x0008)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory Wmi = uint16(0x0009)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory ComPlus = uint16(0x000A)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory StateMachine = uint16(0x000B)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory Wsat = uint16(0x000C)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory SharingService = uint16(0x000D)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogCategory ListenerAdapter = uint16(0x000E)
} // end of class System.ServiceModel.Diagnostics.EventLogCategory

.class private auto ansi sealed System.ServiceModel.Diagnostics.EventLogEventId
       extends [mscorlib]System.Enum
{
  .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 44 54 68 69 73 20 68 61 73 20 62 65 65 6E   // ..DThis has been
                                                                              20 72 65 70 6C 61 63 65 64 20 62 79 20 53 79 73   //  replaced by Sys
                                                                              74 65 6D 2E 52 75 6E 74 69 6D 65 2E 44 69 61 67   // tem.Runtime.Diag
                                                                              6E 6F 73 74 69 63 73 2E 45 76 65 6E 74 4C 6F 67   // nostics.EventLog
                                                                              45 76 65 6E 74 49 64 00 00 )                      // EventId..
  .field public specialname rtspecialname uint32 value__
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId FailedToSetupTracing = uint32(0xC0010064)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId FailedToInitializeTraceSource = uint32(0xC0010065)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId FailFast = uint32(0xC0010066)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId FailFastException = uint32(0xC0010067)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId FailedToTraceEvent = uint32(0xC0010068)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId FailedToTraceEventWithException = uint32(0xC0010069)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId InvariantAssertionFailed = uint32(0xC001006A)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId PiiLoggingOn = uint32(0xC001006B)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId PiiLoggingNotAllowed = uint32(0xC001006C)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WebHostUnhandledException = uint32(0xC0020001)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WebHostHttpError = uint32(0xC0020002)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WebHostFailedToProcessRequest = uint32(0xC0020003)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WebHostFailedToListen = uint32(0xC0020004)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId FailedToLogMessage = uint32(0xC0020005)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId RemovedBadFilter = uint32(0xC0020006)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId FailedToCreateMessageLoggingTraceSource = uint32(0xC0020007)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId MessageLoggingOn = uint32(0xC0020008)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId MessageLoggingOff = uint32(0xC0020009)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId FailedToLoadPerformanceCounter = uint32(0xC002000A)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId FailedToRemovePerformanceCounter = uint32(0xC002000B)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WmiGetObjectFailed = uint32(0xC002000C)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WmiPutInstanceFailed = uint32(0xC002000D)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WmiDeleteInstanceFailed = uint32(0xC002000E)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WmiCreateInstanceFailed = uint32(0xC002000F)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WmiExecQueryFailed = uint32(0xC0020010)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WmiExecMethodFailed = uint32(0xC0020011)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WmiRegistrationFailed = uint32(0xC0020012)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WmiUnregistrationFailed = uint32(0xC0020013)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WmiAdminTypeMismatch = uint32(0xC0020014)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WmiPropertyMissing = uint32(0xC0020015)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ComPlusServiceHostStartingServiceError = uint32(0xC0020016)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ComPlusDllHostInitializerStartingError = uint32(0xC0020017)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ComPlusTLBImportError = uint32(0xC0020018)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ComPlusInvokingMethodFailed = uint32(0xC0020019)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ComPlusInstanceCreationError = uint32(0xC002001A)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ComPlusInvokingMethodFailedMismatchedTransactions = uint32(0xC002001B)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId UnhandledStateMachineExceptionRecordDescription = uint32(0xC0030001)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId FatalUnexpectedStateMachineEvent = uint32(0xC0030002)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ParticipantRecoveryLogEntryCorrupt = uint32(0xC0030003)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId CoordinatorRecoveryLogEntryCorrupt = uint32(0xC0030004)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId CoordinatorRecoveryLogEntryCreationFailure = uint32(0xC0030005)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ParticipantRecoveryLogEntryCreationFailure = uint32(0xC0030006)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ProtocolInitializationFailure = uint32(0xC0030007)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ProtocolStartFailure = uint32(0xC0030008)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ProtocolRecoveryBeginningFailure = uint32(0xC0030009)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ProtocolRecoveryCompleteFailure = uint32(0xC003000A)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId TransactionBridgeRecoveryFailure = uint32(0xC003000B)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ProtocolStopFailure = uint32(0xC003000C)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId NonFatalUnexpectedStateMachineEvent = uint32(0xC003000D)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId PerformanceCounterInitializationFailure = uint32(0xC003000E)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ProtocolRecoveryComplete = uint32(0xC003000F)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ProtocolStopped = uint32(0xC0030010)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ThumbPrintNotFound = uint32(0xC0030011)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ThumbPrintNotValidated = uint32(0xC0030012)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId SslNoPrivateKey = uint32(0xC0030013)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId SslNoAccessiblePrivateKey = uint32(0xC0030014)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId MissingNecessaryKeyUsage = uint32(0xC0030015)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId MissingNecessaryEnhancedKeyUsage = uint32(0xC0030016)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId StartErrorPublish = uint32(0xC0040001)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId BindingError = uint32(0xC0040002)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId LAFailedToListenForApp = uint32(0xC0040003)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId UnknownListenerAdapterError = uint32(0xC0040004)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WasDisconnected = uint32(0xC0040005)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId WasConnectionTimedout = uint32(0xC0040006)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ServiceStartFailed = uint32(0xC0040007)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId MessageQueueDuplicatedSocketLeak = uint32(0xC0040008)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId MessageQueueDuplicatedPipeLeak = uint32(0xC0040009)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId SharingUnhandledException = uint32(0xC004000A)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ServiceAuthorizationSuccess = uint32(0x40060001)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ServiceAuthorizationFailure = uint32(0xC0060002)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId MessageAuthenticationSuccess = uint32(0x40060003)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId MessageAuthenticationFailure = uint32(0xC0060004)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId SecurityNegotiationSuccess = uint32(0x40060005)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId SecurityNegotiationFailure = uint32(0xC0060006)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId TransportAuthenticationSuccess = uint32(0x40060007)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId TransportAuthenticationFailure = uint32(0xC0060008)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ImpersonationSuccess = uint32(0x40060009)
  .field public static literal valuetype System.ServiceModel.Diagnostics.EventLogEventId ImpersonationFailure = uint32(0xC006000A)
} // end of class System.ServiceModel.Diagnostics.EventLogEventId

.class private auto ansi beforefieldinit System.ServiceModel.Diagnostics.EventLogger
       extends [mscorlib]System.Object
{
  .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 40 54 68 69 73 20 68 61 73 20 62 65 65 6E   // ..@This has been
                                                                              20 72 65 70 6C 61 63 65 64 20 62 79 20 53 79 73   //  replaced by Sys
                                                                              74 65 6D 2E 52 75 6E 74 69 6D 65 2E 44 69 61 67   // tem.Runtime.Diag
                                                                              6E 6F 73 74 69 63 73 2E 45 76 65 6E 74 4C 6F 67   // nostics.EventLog
                                                                              67 65 72 00 00 )                                  // ger..
  .field private class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger innerEventLogger
  .method private hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ret
  } // end of method EventLogger::.ctor

  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(string eventLogSourceName,
                               object diagnosticTrace) cil managed
  {
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 47 46 6F 72 20 53 4D 44 69 61 67 6E 6F 73   // ..GFor SMDiagnos
                                                                                74 69 63 73 2E 64 6C 6C 20 75 73 65 20 6F 6E 6C   // tics.dll use onl
                                                                                79 2E 20 43 61 6C 6C 20 44 69 61 67 6E 6F 73 74   // y. Call Diagnost
                                                                                69 63 55 74 69 6C 69 74 79 2E 45 76 65 6E 74 4C   // icUtility.EventL
                                                                                6F 67 20 69 6E 73 74 65 61 64 00 00 )             // og instead..
    // Code size       25 (0x19)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  ldarg.2
    IL_0009:  castclass  [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase
    IL_000e:  newobj     instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::.ctor(string,
                                                                                                                    class [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase)
    IL_0013:  stfld      class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger System.ServiceModel.Diagnostics.EventLogger::innerEventLogger
    IL_0018:  ret
  } // end of method EventLogger::.ctor

  .method assembly hidebysig static class System.ServiceModel.Diagnostics.EventLogger 
          UnsafeCreateEventLogger(string eventLogSourceName,
                                  object diagnosticTrace) cil managed
  {
    .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       26 (0x1a)
    .maxstack  3
    .locals init (class System.ServiceModel.Diagnostics.EventLogger V_0)
    IL_0000:  newobj     instance void System.ServiceModel.Diagnostics.EventLogger::.ctor()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  ldarg.0
    IL_0008:  ldarg.1
    IL_0009:  castclass  [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase
    IL_000e:  call       class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::UnsafeCreateEventLogger(string,
                                                                                                                                                                                                    class [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase)
    IL_0013:  stfld      class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger System.ServiceModel.Diagnostics.EventLogger::innerEventLogger
    IL_0018:  ldloc.0
    IL_0019:  ret
  } // end of method EventLogger::UnsafeCreateEventLogger

  .method assembly hidebysig instance void 
          LogEvent(valuetype [System]System.Diagnostics.TraceEventType 'type',
                   valuetype System.ServiceModel.Diagnostics.EventLogCategory category,
                   valuetype System.ServiceModel.Diagnostics.EventLogEventId eventId,
                   bool shouldTrace,
                   string[] values) cil managed
  {
    .param [5]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger System.ServiceModel.Diagnostics.EventLogger::innerEventLogger
    IL_0006:  ldarg.1
    IL_0007:  ldarg.2
    IL_0008:  ldarg.3
    IL_0009:  ldarg.s    shouldTrace
    IL_000b:  ldarg.s    values
    IL_000d:  callvirt   instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::LogEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                                       uint16,
                                                                                                                       uint32,
                                                                                                                       bool,
                                                                                                                       string[])
    IL_0012:  ret
  } // end of method EventLogger::LogEvent

  .method assembly hidebysig instance void 
          UnsafeLogEvent(valuetype [System]System.Diagnostics.TraceEventType 'type',
                         valuetype System.ServiceModel.Diagnostics.EventLogCategory category,
                         valuetype System.ServiceModel.Diagnostics.EventLogEventId eventId,
                         bool shouldTrace,
                         string[] values) cil managed
  {
    .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
    .param [5]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger System.ServiceModel.Diagnostics.EventLogger::innerEventLogger
    IL_0006:  ldarg.1
    IL_0007:  ldarg.2
    IL_0008:  ldarg.3
    IL_0009:  ldarg.s    shouldTrace
    IL_000b:  ldarg.s    values
    IL_000d:  callvirt   instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::UnsafeLogEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                                             uint16,
                                                                                                                             uint32,
                                                                                                                             bool,
                                                                                                                             string[])
    IL_0012:  ret
  } // end of method EventLogger::UnsafeLogEvent

  .method assembly hidebysig instance void 
          LogEvent(valuetype [System]System.Diagnostics.TraceEventType 'type',
                   valuetype System.ServiceModel.Diagnostics.EventLogCategory category,
                   valuetype System.ServiceModel.Diagnostics.EventLogEventId eventId,
                   string[] values) cil managed
  {
    .param [4]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger System.ServiceModel.Diagnostics.EventLogger::innerEventLogger
    IL_0006:  ldarg.1
    IL_0007:  ldarg.2
    IL_0008:  ldarg.3
    IL_0009:  ldarg.s    values
    IL_000b:  callvirt   instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::LogEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                                       uint16,
                                                                                                                       uint32,
                                                                                                                       string[])
    IL_0010:  ret
  } // end of method EventLogger::LogEvent

  .method assembly hidebysig static string 
          NormalizeEventLogParameter(string param) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       string [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::NormalizeEventLogParameter(string)
    IL_0006:  ret
  } // end of method EventLogger::NormalizeEventLogParameter

} // end of class System.ServiceModel.Diagnostics.EventLogger

.class private auto ansi beforefieldinit System.ServiceModel.Diagnostics.ExceptionUtility
       extends [mscorlib]System.Object
{
  .field private static literal string ExceptionStackAsStringKey = "System.ServiceModel.Diagnostics.ExceptionUtility.E"
  + "xceptionStackAsString"
  .field static assembly class System.ServiceModel.Diagnostics.ExceptionUtility mainInstance
  .field private class System.ServiceModel.Diagnostics.LegacyDiagnosticTrace diagnosticTrace
  .field private class [System.ServiceModel.Internals]System.Runtime.ExceptionTrace exceptionTrace
  .field private string name
  .field private string eventSourceName
  .field private static valuetype [mscorlib]System.Guid activityId
  .custom instance void [mscorlib]System.ThreadStaticAttribute::.ctor() = ( 01 00 00 00 ) 
  .field private static bool useStaticActivityId
  .custom instance void [mscorlib]System.ThreadStaticAttribute::.ctor() = ( 01 00 00 00 ) 
  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(string name,
                               string eventSourceName,
                               object diagnosticTrace,
                               object exceptionTrace) cil managed
  {
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 4F 46 6F 72 20 53 4D 44 69 61 67 6E 6F 73   // ..OFor SMDiagnos
                                                                                74 69 63 73 2E 64 6C 6C 20 75 73 65 20 6F 6E 6C   // tics.dll use onl
                                                                                79 2E 20 43 61 6C 6C 20 44 69 61 67 6E 6F 73 74   // y. Call Diagnost
                                                                                69 63 55 74 69 6C 69 74 79 2E 45 78 63 65 70 74   // icUtility.Except
                                                                                69 6F 6E 55 74 69 6C 69 74 79 20 69 6E 73 74 65   // ionUtility inste
                                                                                61 64 00 00 )                                     // ad..
    // Code size       46 (0x2e)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.3
    IL_0008:  castclass  System.ServiceModel.Diagnostics.LegacyDiagnosticTrace
    IL_000d:  stfld      class System.ServiceModel.Diagnostics.LegacyDiagnosticTrace System.ServiceModel.Diagnostics.ExceptionUtility::diagnosticTrace
    IL_0012:  ldarg.0
    IL_0013:  ldarg.s    exceptionTrace
    IL_0015:  castclass  [System.ServiceModel.Internals]System.Runtime.ExceptionTrace
    IL_001a:  stfld      class [System.ServiceModel.Internals]System.Runtime.ExceptionTrace System.ServiceModel.Diagnostics.ExceptionUtility::exceptionTrace
    IL_001f:  ldarg.0
    IL_0020:  ldarg.1
    IL_0021:  stfld      string System.ServiceModel.Diagnostics.ExceptionUtility::name
    IL_0026:  ldarg.0
    IL_0027:  ldarg.2
    IL_0028:  stfld      string System.ServiceModel.Diagnostics.ExceptionUtility::eventSourceName
    IL_002d:  ret
  } // end of method ExceptionUtility::.ctor

  .method assembly hidebysig instance void 
          TraceFailFast(string message) cil managed noinlining
  {
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 4F 46 6F 72 20 53 4D 44 69 61 67 6E 6F 73   // ..OFor SMDiagnos
                                                                                74 69 63 73 2E 64 6C 6C 20 75 73 65 20 6F 6E 6C   // tics.dll use onl
                                                                                79 2E 20 43 61 6C 6C 20 44 69 61 67 6E 6F 73 74   // y. Call Diagnost
                                                                                69 63 55 74 69 6C 69 74 79 2E 45 78 63 65 70 74   // icUtility.Except
                                                                                69 6F 6E 55 74 69 6C 69 74 79 20 69 6E 73 74 65   // ionUtility inste
                                                                                61 64 00 00 )                                     // ad..
    // Code size       31 (0x1f)
    .maxstack  2
    .locals init (class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger V_0)
    IL_0000:  ldnull
    IL_0001:  stloc.0
    .try
    {
      IL_0002:  ldarg.0
      IL_0003:  ldfld      string System.ServiceModel.Diagnostics.ExceptionUtility::eventSourceName
      IL_0008:  ldarg.0
      IL_0009:  ldfld      class System.ServiceModel.Diagnostics.LegacyDiagnosticTrace System.ServiceModel.Diagnostics.ExceptionUtility::diagnosticTrace
      IL_000e:  newobj     instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::.ctor(string,
                                                                                                                      class [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase)
      IL_0013:  stloc.0
      IL_0014:  leave.s    IL_001e

    }  // end .try
    finally
    {
      IL_0016:  ldarg.1
      IL_0017:  ldloc.0
      IL_0018:  call       void System.ServiceModel.Diagnostics.ExceptionUtility::TraceFailFast(string,
                                                                                                class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger)
      IL_001d:  endfinally
    }  // end handler
    IL_001e:  ret
  } // end of method ExceptionUtility::TraceFailFast

  .method assembly hidebysig static void 
          TraceFailFast(string message,
                        class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger logger) cil managed noinlining
  {
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 4F 46 6F 72 20 53 4D 44 69 61 67 6E 6F 73   // ..OFor SMDiagnos
                                                                                74 69 63 73 2E 64 6C 6C 20 75 73 65 20 6F 6E 6C   // tics.dll use onl
                                                                                79 2E 20 43 61 6C 6C 20 44 69 61 67 6E 6F 73 74   // y. Call Diagnost
                                                                                69 63 55 74 69 6C 69 74 79 2E 45 78 63 65 70 74   // icUtility.Except
                                                                                69 6F 6E 55 74 69 6C 69 74 79 20 69 6E 73 74 65   // ionUtility inste
                                                                                61 64 00 00 )                                     // ad..
    // Code size       95 (0x5f)
    .maxstack  8
    .locals init (string V_0,
             class [mscorlib]System.Exception V_1,
             class [mscorlib]System.Exception V_2)
    .try
    {
      IL_0000:  ldarg.1
      IL_0001:  brfalse.s  IL_003a

      IL_0003:  ldnull
      IL_0004:  stloc.0
      .try
      {
        .try
        {
          IL_0005:  newobj     instance void [mscorlib]System.Diagnostics.StackTrace::.ctor()
          IL_000a:  callvirt   instance string [mscorlib]System.Object::ToString()
          IL_000f:  stloc.0
          IL_0010:  leave.s    IL_001c

        }  // end .try
        catch [mscorlib]System.Exception 
        {
          IL_0012:  stloc.1
          IL_0013:  ldloc.1
          IL_0014:  callvirt   instance string [mscorlib]System.Exception::get_Message()
          IL_0019:  stloc.0
          IL_001a:  leave.s    IL_001c

        }  // end handler
        IL_001c:  leave.s    IL_003a

      }  // end .try
      finally
      {
        IL_001e:  ldarg.1
        IL_001f:  ldc.i4.1
        IL_0020:  ldc.i4.6
        IL_0021:  ldc.i4     0xc0010066
        IL_0026:  ldc.i4.2
        IL_0027:  newarr     [mscorlib]System.String
        IL_002c:  dup
        IL_002d:  ldc.i4.0
        IL_002e:  ldarg.0
        IL_002f:  stelem.ref
        IL_0030:  dup
        IL_0031:  ldc.i4.1
        IL_0032:  ldloc.0
        IL_0033:  stelem.ref
        IL_0034:  callvirt   instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::LogEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                                           uint16,
                                                                                                                           uint32,
                                                                                                                           string[])
        IL_0039:  endfinally
      }  // end handler
      IL_003a:  leave.s    IL_005e

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_003c:  stloc.2
      IL_003d:  ldarg.1
      IL_003e:  brfalse.s  IL_005c

      IL_0040:  ldarg.1
      IL_0041:  ldc.i4.1
      IL_0042:  ldc.i4.6
      IL_0043:  ldc.i4     0xc0010067
      IL_0048:  ldc.i4.1
      IL_0049:  newarr     [mscorlib]System.String
      IL_004e:  dup
      IL_004f:  ldc.i4.0
      IL_0050:  ldloc.2
      IL_0051:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_0056:  stelem.ref
      IL_0057:  callvirt   instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::LogEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                                         uint16,
                                                                                                                         uint32,
                                                                                                                         string[])
      IL_005c:  rethrow
    }  // end handler
    IL_005e:  ret
  } // end of method ExceptionUtility::TraceFailFast

  .method assembly hidebysig instance void 
          TraceFailFastException(class [mscorlib]System.Exception exception) cil managed
  {
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 4F 46 6F 72 20 53 4D 44 69 61 67 6E 6F 73   // ..OFor SMDiagnos
                                                                                74 69 63 73 2E 64 6C 6C 20 75 73 65 20 6F 6E 6C   // tics.dll use onl
                                                                                79 2E 20 43 61 6C 6C 20 44 69 61 67 6E 6F 73 74   // y. Call Diagnost
                                                                                69 63 55 74 69 6C 69 74 79 2E 45 78 63 65 70 74   // icUtility.Except
                                                                                69 6F 6E 55 74 69 6C 69 74 79 20 69 6E 73 74 65   // ionUtility inste
                                                                                61 64 00 00 )                                     // ad..
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  brfalse.s  IL_000c

    IL_0004:  ldarg.1
    IL_0005:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_000a:  br.s       IL_000d

    IL_000c:  ldnull
    IL_000d:  call       instance void System.ServiceModel.Diagnostics.ExceptionUtility::TraceFailFast(string)
    IL_0012:  ret
  } // end of method ExceptionUtility::TraceFailFastException

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelper(class [mscorlib]System.Exception exception,
                      valuetype [System]System.Diagnostics.TraceEventType eventType,
                      class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord extendedData) cil managed
  {
    // Code size       309 (0x135)
    .maxstack  8
    .locals init (bool V_0,
             class [mscorlib]System.Collections.IDictionary V_1,
             class System.ServiceModel.Diagnostics.Activity V_2,
             object V_3,
             string V_4,
             string V_5)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.LegacyDiagnosticTrace System.ServiceModel.Diagnostics.ExceptionUtility::diagnosticTrace
    IL_0006:  brfalse.s  IL_0016

    IL_0008:  ldarg.0
    IL_0009:  ldfld      class System.ServiceModel.Diagnostics.LegacyDiagnosticTrace System.ServiceModel.Diagnostics.ExceptionUtility::diagnosticTrace
    IL_000e:  ldarg.2
    IL_000f:  callvirt   instance bool [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::ShouldTrace(valuetype [System]System.Diagnostics.TraceEventType)
    IL_0014:  br.s       IL_0017

    IL_0016:  ldc.i4.0
    IL_0017:  stloc.0
    IL_0018:  ldloc.0
    IL_0019:  brfalse    IL_0126

    IL_001e:  ldsfld     bool System.ServiceModel.Diagnostics.ExceptionUtility::useStaticActivityId
    IL_0023:  brtrue.s   IL_0028

    IL_0025:  ldnull
    IL_0026:  br.s       IL_0032

    IL_0028:  ldsfld     valuetype [mscorlib]System.Guid System.ServiceModel.Diagnostics.ExceptionUtility::activityId
    IL_002d:  call       class System.ServiceModel.Diagnostics.Activity System.ServiceModel.Diagnostics.Activity::CreateActivity(valuetype [mscorlib]System.Guid)
    IL_0032:  stloc.2
    .try
    {
      IL_0033:  ldarg.0
      IL_0034:  ldfld      class System.ServiceModel.Diagnostics.LegacyDiagnosticTrace System.ServiceModel.Diagnostics.ExceptionUtility::diagnosticTrace
      IL_0039:  ldarg.2
      IL_003a:  ldc.i4     0x20003
      IL_003f:  ldstr      "System.ServiceModel.Diagnostics"
      IL_0044:  ldstr      "ThrowingException"
      IL_0049:  call       string System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::GenerateMsdnTraceCode(string,
                                                                                                               string)
      IL_004e:  ldstr      "ThrowingException"
      IL_0053:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
      IL_0058:  ldarg.3
      IL_0059:  ldarg.1
      IL_005a:  ldnull
      IL_005b:  callvirt   instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::TraceEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                           int32,
                                                                                                           string,
                                                                                                           string,
                                                                                                           class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord,
                                                                                                           class [mscorlib]System.Exception,
                                                                                                           object)
      IL_0060:  leave.s    IL_006c

    }  // end .try
    finally
    {
      IL_0062:  ldloc.2
      IL_0063:  brfalse.s  IL_006b

      IL_0065:  ldloc.2
      IL_0066:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_006b:  endfinally
    }  // end handler
    IL_006c:  ldarg.1
    IL_006d:  callvirt   instance class [mscorlib]System.Collections.IDictionary [mscorlib]System.Exception::get_Data()
    IL_0072:  stloc.1
    IL_0073:  ldloc.1
    IL_0074:  brfalse    IL_0126

    IL_0079:  ldloc.1
    IL_007a:  callvirt   instance bool [mscorlib]System.Collections.IDictionary::get_IsReadOnly()
    IL_007f:  brtrue     IL_0126

    IL_0084:  ldloc.1
    IL_0085:  callvirt   instance bool [mscorlib]System.Collections.IDictionary::get_IsFixedSize()
    IL_008a:  brtrue     IL_0126

    IL_008f:  ldloc.1
    IL_0090:  ldstr      "System.ServiceModel.Diagnostics.ExceptionUtility.E"
    + "xceptionStackAsString"
    IL_0095:  callvirt   instance object [mscorlib]System.Collections.IDictionary::get_Item(object)
    IL_009a:  stloc.3
    IL_009b:  ldloc.3
    IL_009c:  brfalse.s  IL_00a6

    IL_009e:  ldloc.3
    IL_009f:  isinst     [mscorlib]System.String
    IL_00a4:  br.s       IL_00ab

    IL_00a6:  ldstr      ""
    IL_00ab:  stloc.s    V_4
    IL_00ad:  ldloc.s    V_4
    IL_00af:  brfalse.s  IL_0126

    IL_00b1:  ldarg.1
    IL_00b2:  callvirt   instance string [mscorlib]System.Exception::get_StackTrace()
    IL_00b7:  stloc.s    V_5
    IL_00b9:  ldloc.s    V_5
    IL_00bb:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
    IL_00c0:  brtrue.s   IL_0126

    IL_00c2:  ldc.i4.8
    IL_00c3:  newarr     [mscorlib]System.String
    IL_00c8:  dup
    IL_00c9:  ldc.i4.0
    IL_00ca:  ldloc.s    V_4
    IL_00cc:  stelem.ref
    IL_00cd:  dup
    IL_00ce:  ldc.i4.1
    IL_00cf:  ldloc.s    V_4
    IL_00d1:  callvirt   instance int32 [mscorlib]System.String::get_Length()
    IL_00d6:  brfalse.s  IL_00df

    IL_00d8:  call       string [mscorlib]System.Environment::get_NewLine()
    IL_00dd:  br.s       IL_00e4

    IL_00df:  ldstr      ""
    IL_00e4:  stelem.ref
    IL_00e5:  dup
    IL_00e6:  ldc.i4.2
    IL_00e7:  ldstr      "throw"
    IL_00ec:  stelem.ref
    IL_00ed:  dup
    IL_00ee:  ldc.i4.3
    IL_00ef:  call       string [mscorlib]System.Environment::get_NewLine()
    IL_00f4:  stelem.ref
    IL_00f5:  dup
    IL_00f6:  ldc.i4.4
    IL_00f7:  ldloc.s    V_5
    IL_00f9:  stelem.ref
    IL_00fa:  dup
    IL_00fb:  ldc.i4.5
    IL_00fc:  call       string [mscorlib]System.Environment::get_NewLine()
    IL_0101:  stelem.ref
    IL_0102:  dup
    IL_0103:  ldc.i4.6
    IL_0104:  ldstr      "catch"
    IL_0109:  stelem.ref
    IL_010a:  dup
    IL_010b:  ldc.i4.7
    IL_010c:  call       string [mscorlib]System.Environment::get_NewLine()
    IL_0111:  stelem.ref
    IL_0112:  call       string [mscorlib]System.String::Concat(string[])
    IL_0117:  stloc.s    V_4
    IL_0119:  ldloc.1
    IL_011a:  ldstr      "System.ServiceModel.Diagnostics.ExceptionUtility.E"
    + "xceptionStackAsString"
    IL_011f:  ldloc.s    V_4
    IL_0121:  callvirt   instance void [mscorlib]System.Collections.IDictionary::set_Item(object,
                                                                                          object)
    IL_0126:  ldarg.0
    IL_0127:  ldfld      class [System.ServiceModel.Internals]System.Runtime.ExceptionTrace System.ServiceModel.Diagnostics.ExceptionUtility::exceptionTrace
    IL_012c:  ldarg.1
    IL_012d:  ldarg.2
    IL_012e:  callvirt   instance void [System.ServiceModel.Internals]System.Runtime.ExceptionTrace::TraceEtwException(class [mscorlib]System.Exception,
                                                                                                                       valuetype [System]System.Diagnostics.TraceEventType)
    IL_0133:  ldarg.1
    IL_0134:  ret
  } // end of method ExceptionUtility::ThrowHelper

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelper(class [mscorlib]System.Exception exception,
                      valuetype [System]System.Diagnostics.TraceEventType eventType) cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  ldnull
    IL_0004:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelper(class [mscorlib]System.Exception,
                                                                                                                                 valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                                                 class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord)
    IL_0009:  ret
  } // end of method ExceptionUtility::ThrowHelper

  .method assembly hidebysig instance class [mscorlib]System.ArgumentException 
          ThrowHelperArgument(string message) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void [mscorlib]System.ArgumentException::.ctor(string)
    IL_0007:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelperError(class [mscorlib]System.Exception)
    IL_000c:  castclass  [mscorlib]System.ArgumentException
    IL_0011:  ret
  } // end of method ExceptionUtility::ThrowHelperArgument

  .method assembly hidebysig instance class [mscorlib]System.ArgumentException 
          ThrowHelperArgument(string paramName,
                              string message) cil managed
  {
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.2
    IL_0002:  ldarg.1
    IL_0003:  newobj     instance void [mscorlib]System.ArgumentException::.ctor(string,
                                                                                 string)
    IL_0008:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelperError(class [mscorlib]System.Exception)
    IL_000d:  castclass  [mscorlib]System.ArgumentException
    IL_0012:  ret
  } // end of method ExceptionUtility::ThrowHelperArgument

  .method assembly hidebysig instance class [mscorlib]System.ArgumentNullException 
          ThrowHelperArgumentNull(string paramName) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void [mscorlib]System.ArgumentNullException::.ctor(string)
    IL_0007:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelperError(class [mscorlib]System.Exception)
    IL_000c:  castclass  [mscorlib]System.ArgumentNullException
    IL_0011:  ret
  } // end of method ExceptionUtility::ThrowHelperArgumentNull

  .method assembly hidebysig instance class [mscorlib]System.ArgumentNullException 
          ThrowHelperArgumentNull(string paramName,
                                  string message) cil managed
  {
    // Code size       19 (0x13)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  newobj     instance void [mscorlib]System.ArgumentNullException::.ctor(string,
                                                                                     string)
    IL_0008:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelperError(class [mscorlib]System.Exception)
    IL_000d:  castclass  [mscorlib]System.ArgumentNullException
    IL_0012:  ret
  } // end of method ExceptionUtility::ThrowHelperArgumentNull

  .method assembly hidebysig instance class [mscorlib]System.ArgumentException 
          ThrowHelperArgumentNullOrEmptyString(string arg) cil managed
  {
    // Code size       28 (0x1c)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldstr      "StringNullOrEmpty"
    IL_0006:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
    IL_000b:  ldarg.1
    IL_000c:  newobj     instance void [mscorlib]System.ArgumentException::.ctor(string,
                                                                                 string)
    IL_0011:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelperError(class [mscorlib]System.Exception)
    IL_0016:  castclass  [mscorlib]System.ArgumentException
    IL_001b:  ret
  } // end of method ExceptionUtility::ThrowHelperArgumentNullOrEmptyString

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelperFatal(string message,
                           class [mscorlib]System.Exception innerException) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  newobj     instance void [System.ServiceModel.Internals]System.Runtime.FatalException::.ctor(string,
                                                                                                           class [mscorlib]System.Exception)
    IL_0008:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelperError(class [mscorlib]System.Exception)
    IL_000d:  ret
  } // end of method ExceptionUtility::ThrowHelperFatal

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelperInternal(bool fatal) cil managed
  {
    // Code size       25 (0x19)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000e

    IL_0003:  ldstr      "InternalException should never be thrown."
    IL_0008:  call       class [mscorlib]System.Exception [System.ServiceModel.Internals]System.Runtime.Fx::AssertAndThrow(string)
    IL_000d:  ret

    IL_000e:  ldstr      "Fatal InternalException should never be thrown."
    IL_0013:  call       class [mscorlib]System.Exception [System.ServiceModel.Internals]System.Runtime.Fx::AssertAndThrowFatal(string)
    IL_0018:  ret
  } // end of method ExceptionUtility::ThrowHelperInternal

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelperInvalidOperation(string message) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor(string)
    IL_0007:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelperError(class [mscorlib]System.Exception)
    IL_000c:  ret
  } // end of method ExceptionUtility::ThrowHelperInvalidOperation

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelperCallback(string message,
                              class [mscorlib]System.Exception innerException) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  newobj     instance void [System.ServiceModel.Internals]System.Runtime.CallbackException::.ctor(string,
                                                                                                              class [mscorlib]System.Exception)
    IL_0008:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelperCritical(class [mscorlib]System.Exception)
    IL_000d:  ret
  } // end of method ExceptionUtility::ThrowHelperCallback

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelperCallback(class [mscorlib]System.Exception innerException) cil managed
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldstr      "GenericCallbackException"
    IL_0006:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
    IL_000b:  ldarg.1
    IL_000c:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelperCallback(string,
                                                                                                                                         class [mscorlib]System.Exception)
    IL_0011:  ret
  } // end of method ExceptionUtility::ThrowHelperCallback

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelperCritical(class [mscorlib]System.Exception exception) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldc.i4.1
    IL_0003:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelper(class [mscorlib]System.Exception,
                                                                                                                                 valuetype [System]System.Diagnostics.TraceEventType)
    IL_0008:  ret
  } // end of method ExceptionUtility::ThrowHelperCritical

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelperError(class [mscorlib]System.Exception exception) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldc.i4.2
    IL_0003:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelper(class [mscorlib]System.Exception,
                                                                                                                                 valuetype [System]System.Diagnostics.TraceEventType)
    IL_0008:  ret
  } // end of method ExceptionUtility::ThrowHelperError

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelperWarning(class [mscorlib]System.Exception exception) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldc.i4.4
    IL_0003:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelper(class [mscorlib]System.Exception,
                                                                                                                                 valuetype [System]System.Diagnostics.TraceEventType)
    IL_0008:  ret
  } // end of method ExceptionUtility::ThrowHelperWarning

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelperXml(class [System.Xml]System.Xml.XmlReader reader,
                         string message) cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  ldnull
    IL_0004:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelperXml(class [System.Xml]System.Xml.XmlReader,
                                                                                                                                    string,
                                                                                                                                    class [mscorlib]System.Exception)
    IL_0009:  ret
  } // end of method ExceptionUtility::ThrowHelperXml

  .method assembly hidebysig instance class [mscorlib]System.Exception 
          ThrowHelperXml(class [System.Xml]System.Xml.XmlReader reader,
                         string message,
                         class [mscorlib]System.Exception inner) cil managed
  {
    // Code size       45 (0x2d)
    .maxstack  5
    .locals init (class [System.Xml]System.Xml.IXmlLineInfo V_0)
    IL_0000:  ldarg.1
    IL_0001:  isinst     [System.Xml]System.Xml.IXmlLineInfo
    IL_0006:  stloc.0
    IL_0007:  ldarg.0
    IL_0008:  ldarg.2
    IL_0009:  ldarg.3
    IL_000a:  ldloc.0
    IL_000b:  brtrue.s   IL_0010

    IL_000d:  ldc.i4.0
    IL_000e:  br.s       IL_0016

    IL_0010:  ldloc.0
    IL_0011:  callvirt   instance int32 [System.Xml]System.Xml.IXmlLineInfo::get_LineNumber()
    IL_0016:  ldloc.0
    IL_0017:  brtrue.s   IL_001c

    IL_0019:  ldc.i4.0
    IL_001a:  br.s       IL_0022

    IL_001c:  ldloc.0
    IL_001d:  callvirt   instance int32 [System.Xml]System.Xml.IXmlLineInfo::get_LinePosition()
    IL_0022:  newobj     instance void [System.Xml]System.Xml.XmlException::.ctor(string,
                                                                                  class [mscorlib]System.Exception,
                                                                                  int32,
                                                                                  int32)
    IL_0027:  call       instance class [mscorlib]System.Exception System.ServiceModel.Diagnostics.ExceptionUtility::ThrowHelperError(class [mscorlib]System.Exception)
    IL_002c:  ret
  } // end of method ExceptionUtility::ThrowHelperXml

  .method assembly hidebysig instance void 
          DiagnosticTraceHandledException(class [mscorlib]System.Exception exception,
                                          valuetype [System]System.Diagnostics.TraceEventType eventType) cil managed
  {
    // Code size       106 (0x6a)
    .maxstack  8
    .locals init (bool V_0,
             class System.ServiceModel.Diagnostics.Activity V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.LegacyDiagnosticTrace System.ServiceModel.Diagnostics.ExceptionUtility::diagnosticTrace
    IL_0006:  brfalse.s  IL_0016

    IL_0008:  ldarg.0
    IL_0009:  ldfld      class System.ServiceModel.Diagnostics.LegacyDiagnosticTrace System.ServiceModel.Diagnostics.ExceptionUtility::diagnosticTrace
    IL_000e:  ldarg.2
    IL_000f:  callvirt   instance bool [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::ShouldTrace(valuetype [System]System.Diagnostics.TraceEventType)
    IL_0014:  br.s       IL_0017

    IL_0016:  ldc.i4.0
    IL_0017:  stloc.0
    IL_0018:  ldloc.0
    IL_0019:  brfalse.s  IL_0069

    IL_001b:  ldsfld     bool System.ServiceModel.Diagnostics.ExceptionUtility::useStaticActivityId
    IL_0020:  brtrue.s   IL_0025

    IL_0022:  ldnull
    IL_0023:  br.s       IL_002f

    IL_0025:  ldsfld     valuetype [mscorlib]System.Guid System.ServiceModel.Diagnostics.ExceptionUtility::activityId
    IL_002a:  call       class System.ServiceModel.Diagnostics.Activity System.ServiceModel.Diagnostics.Activity::CreateActivity(valuetype [mscorlib]System.Guid)
    IL_002f:  stloc.1
    .try
    {
      IL_0030:  ldarg.0
      IL_0031:  ldfld      class System.ServiceModel.Diagnostics.LegacyDiagnosticTrace System.ServiceModel.Diagnostics.ExceptionUtility::diagnosticTrace
      IL_0036:  ldarg.2
      IL_0037:  ldc.i4     0x20004
      IL_003c:  ldstr      "System.ServiceModel.Diagnostics"
      IL_0041:  ldstr      "TraceHandledException"
      IL_0046:  call       string System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::GenerateMsdnTraceCode(string,
                                                                                                               string)
      IL_004b:  ldstr      "TraceHandledException"
      IL_0050:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
      IL_0055:  ldnull
      IL_0056:  ldarg.1
      IL_0057:  ldnull
      IL_0058:  callvirt   instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::TraceEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                           int32,
                                                                                                           string,
                                                                                                           string,
                                                                                                           class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord,
                                                                                                           class [mscorlib]System.Exception,
                                                                                                           object)
      IL_005d:  leave.s    IL_0069

    }  // end .try
    finally
    {
      IL_005f:  ldloc.1
      IL_0060:  brfalse.s  IL_0068

      IL_0062:  ldloc.1
      IL_0063:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_0068:  endfinally
    }  // end handler
    IL_0069:  ret
  } // end of method ExceptionUtility::DiagnosticTraceHandledException

  .method assembly hidebysig static void 
          UseActivityId(valuetype [mscorlib]System.Guid activityId) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  stsfld     valuetype [mscorlib]System.Guid System.ServiceModel.Diagnostics.ExceptionUtility::activityId
    IL_0006:  ldc.i4.1
    IL_0007:  stsfld     bool System.ServiceModel.Diagnostics.ExceptionUtility::useStaticActivityId
    IL_000c:  ret
  } // end of method ExceptionUtility::UseActivityId

  .method assembly hidebysig static void 
          ClearActivityId() cil managed
  {
    // Code size       17 (0x11)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  stsfld     bool System.ServiceModel.Diagnostics.ExceptionUtility::useStaticActivityId
    IL_0006:  ldsfld     valuetype [mscorlib]System.Guid [mscorlib]System.Guid::Empty
    IL_000b:  stsfld     valuetype [mscorlib]System.Guid System.ServiceModel.Diagnostics.ExceptionUtility::activityId
    IL_0010:  ret
  } // end of method ExceptionUtility::ClearActivityId

  .method assembly hidebysig static bool 
          IsInfrastructureException(class [mscorlib]System.Exception exception) cil managed
  {
    .custom instance void [mscorlib]System.Runtime.ConstrainedExecution.ReliabilityContractAttribute::.ctor(valuetype [mscorlib]System.Runtime.ConstrainedExecution.Consistency,
                                                                                                            valuetype [mscorlib]System.Runtime.ConstrainedExecution.Cer) = ( 01 00 03 00 00 00 02 00 00 00 00 00 ) 
    // Code size       25 (0x19)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  brfalse.s  IL_0017

    IL_0003:  ldarg.0
    IL_0004:  isinst     [mscorlib]System.Threading.ThreadAbortException
    IL_0009:  brtrue.s   IL_0015

    IL_000b:  ldarg.0
    IL_000c:  isinst     [mscorlib]System.AppDomainUnloadedException
    IL_0011:  ldnull
    IL_0012:  cgt.un
    IL_0014:  ret

    IL_0015:  ldc.i4.1
    IL_0016:  ret

    IL_0017:  ldc.i4.0
    IL_0018:  ret
  } // end of method ExceptionUtility::IsInfrastructureException

} // end of class System.ServiceModel.Diagnostics.ExceptionUtility

.class private auto ansi beforefieldinit System.ServiceModel.Diagnostics.LegacyDiagnosticTrace
       extends [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase
{
  .field private static literal int32 MaxTraceSize = int32(0x0000FFFF)
  .field private bool shouldUseActivity
  .field private valuetype System.ServiceModel.Diagnostics.TraceSourceKind traceSourceType
  .field private static literal string subType = ""
  .field private static literal string version = "1"
  .field private static literal int32 traceFailureLogThreshold = int32(0x00000001)
  .field private static literal valuetype [System]System.Diagnostics.SourceLevels DefaultLevel = int32(0x00000000)
  .field private static object classLockObject
  .method family hidebysig virtual instance void 
          OnSetLevel(valuetype [System]System.Diagnostics.SourceLevels level) cil managed
  {
    // Code size       65 (0x41)
    .maxstack  3
    .locals init (class [System]System.Diagnostics.TraceSource V_0)
    IL_0000:  ldarg.0
    IL_0001:  call       instance class [System]System.Diagnostics.TraceSource [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_TraceSource()
    IL_0006:  brfalse.s  IL_0040

    IL_0008:  ldarg.0
    IL_0009:  call       instance class [System]System.Diagnostics.TraceSource [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_TraceSource()
    IL_000e:  callvirt   instance class [System]System.Diagnostics.SourceSwitch [System]System.Diagnostics.TraceSource::get_Switch()
    IL_0013:  callvirt   instance valuetype [System]System.Diagnostics.SourceLevels [System]System.Diagnostics.SourceSwitch::get_Level()
    IL_0018:  brfalse.s  IL_0030

    IL_001a:  ldarg.1
    IL_001b:  brtrue.s   IL_0030

    IL_001d:  ldarg.0
    IL_001e:  call       instance class [System]System.Diagnostics.TraceSource [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_TraceSource()
    IL_0023:  stloc.0
    IL_0024:  ldarg.0
    IL_0025:  call       instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::CreateTraceSource()
    IL_002a:  ldloc.0
    IL_002b:  callvirt   instance void [System]System.Diagnostics.TraceSource::Close()
    IL_0030:  ldarg.0
    IL_0031:  ldarg.1
    IL_0032:  ldc.i4     0xff00
    IL_0037:  and
    IL_0038:  ldc.i4.0
    IL_0039:  cgt.un
    IL_003b:  stfld      bool System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::shouldUseActivity
    IL_0040:  ret
  } // end of method LegacyDiagnosticTrace::OnSetLevel

  .method assembly hidebysig specialname 
          instance bool  get_ShouldUseActivity() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::shouldUseActivity
    IL_0006:  ret
  } // end of method LegacyDiagnosticTrace::get_ShouldUseActivity

  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(valuetype System.ServiceModel.Diagnostics.TraceSourceKind sourceType,
                               string traceSourceName,
                               string eventSourceName) cil managed
  {
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 5A 46 6F 72 20 53 4D 44 69 61 67 6E 6F 73   // ..ZFor SMDiagnos
                                                                                74 69 63 73 2E 64 6C 6C 20 75 73 65 20 6F 6E 6C   // tics.dll use onl
                                                                                79 2E 20 4E 65 76 65 72 20 27 6E 65 77 27 20 74   // y. Never 'new' t
                                                                                68 69 73 20 74 79 70 65 20 75 70 20 75 6E 6C 65   // his type up unle
                                                                                73 73 20 79 6F 75 20 61 72 65 20 44 69 61 67 6E   // ss you are Diagn
                                                                                6F 73 74 69 63 55 74 69 6C 69 74 79 2E 00 00 )    // osticUtility...
    .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       101 (0x65)
    .maxstack  9
    .locals init (class [mscorlib]System.Exception V_0,
             class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  stfld      valuetype System.ServiceModel.Diagnostics.TraceSourceKind System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::traceSourceType
    IL_0007:  ldarg.0
    IL_0008:  ldarg.2
    IL_0009:  call       instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::.ctor(string)
    IL_000e:  ldarg.0
    IL_000f:  ldarg.1
    IL_0010:  stfld      valuetype System.ServiceModel.Diagnostics.TraceSourceKind System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::traceSourceType
    IL_0015:  ldarg.0
    IL_0016:  ldarg.3
    IL_0017:  call       instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::set_EventSourceName(string)
    .try
    {
      IL_001c:  ldarg.0
      IL_001d:  call       instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::CreateTraceSource()
      IL_0022:  ldarg.0
      IL_0023:  call       instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::AddDomainEventHandlersForCleanup()
      IL_0028:  leave.s    IL_0064

    }  // end .try
    catch [System.Configuration]System.Configuration.ConfigurationErrorsException 
    {
      IL_002a:  pop
      IL_002b:  rethrow
    }  // end handler
    catch [mscorlib]System.Exception 
    {
      IL_002d:  stloc.0
      IL_002e:  ldloc.0
      IL_002f:  call       bool [System.ServiceModel.Internals]System.Runtime.Fx::IsFatal(class [mscorlib]System.Exception)
      IL_0034:  brfalse.s  IL_0038

      IL_0036:  rethrow
      IL_0038:  ldarg.0
      IL_0039:  call       instance string [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_EventSourceName()
      IL_003e:  ldnull
      IL_003f:  newobj     instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::.ctor(string,
                                                                                                                      class [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase)
      IL_0044:  stloc.1
      IL_0045:  ldloc.1
      IL_0046:  ldc.i4.2
      IL_0047:  ldc.i4.4
      IL_0048:  ldc.i4     0xc0010064
      IL_004d:  ldc.i4.0
      IL_004e:  ldc.i4.1
      IL_004f:  newarr     [mscorlib]System.String
      IL_0054:  dup
      IL_0055:  ldc.i4.0
      IL_0056:  ldloc.0
      IL_0057:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_005c:  stelem.ref
      IL_005d:  callvirt   instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::LogEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                                         uint16,
                                                                                                                         uint32,
                                                                                                                         bool,
                                                                                                                         string[])
      IL_0062:  leave.s    IL_0064

    }  // end handler
    IL_0064:  ret
  } // end of method LegacyDiagnosticTrace::.ctor

  .method private hidebysig instance void 
          CreateTraceSource() cil managed
  {
    .custom instance void [mscorlib]System.Security.SecuritySafeCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       59 (0x3b)
    .maxstack  3
    .locals init (class System.ServiceModel.Diagnostics.PiiTraceSource V_0)
    IL_0000:  ldnull
    IL_0001:  stloc.0
    IL_0002:  ldarg.0
    IL_0003:  ldfld      valuetype System.ServiceModel.Diagnostics.TraceSourceKind System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::traceSourceType
    IL_0008:  ldc.i4.1
    IL_0009:  bne.un.s   IL_0020

    IL_000b:  ldarg.0
    IL_000c:  ldfld      string [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::TraceSourceName
    IL_0011:  ldarg.0
    IL_0012:  call       instance string [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_EventSourceName()
    IL_0017:  ldc.i4.0
    IL_0018:  newobj     instance void System.ServiceModel.Diagnostics.PiiTraceSource::.ctor(string,
                                                                                             string,
                                                                                             valuetype [System]System.Diagnostics.SourceLevels)
    IL_001d:  stloc.0
    IL_001e:  br.s       IL_0033

    IL_0020:  ldarg.0
    IL_0021:  ldfld      string [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::TraceSourceName
    IL_0026:  ldarg.0
    IL_0027:  call       instance string [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_EventSourceName()
    IL_002c:  ldc.i4.0
    IL_002d:  newobj     instance void System.ServiceModel.Diagnostics.DiagnosticTraceSource::.ctor(string,
                                                                                                    string,
                                                                                                    valuetype [System]System.Diagnostics.SourceLevels)
    IL_0032:  stloc.0
    IL_0033:  ldarg.0
    IL_0034:  ldloc.0
    IL_0035:  call       instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::SetTraceSource(class [System]System.Diagnostics.TraceSource)
    IL_003a:  ret
  } // end of method LegacyDiagnosticTrace::CreateTraceSource

  .method assembly hidebysig instance void 
          TraceEvent(valuetype [System]System.Diagnostics.TraceEventType 'type',
                     int32 code,
                     string msdnTraceCode,
                     string description,
                     class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord trace,
                     class [mscorlib]System.Exception exception,
                     object source) cil managed
  {
    // Code size       176 (0xb0)
    .maxstack  8
    .locals init (class System.ServiceModel.Diagnostics.TraceXPathNavigator V_0,
             class [System.ServiceModel.Internals]System.Runtime.Diagnostics.StringTraceRecord V_1,
             class [mscorlib]System.Exception V_2)
    IL_0000:  ldnull
    IL_0001:  stloc.0
    .try
    {
      IL_0002:  ldarg.0
      IL_0003:  call       instance class [System]System.Diagnostics.TraceSource [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_TraceSource()
      IL_0008:  brfalse.s  IL_0089

      IL_000a:  ldarg.0
      IL_000b:  call       instance bool [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_HaveListeners()
      IL_0010:  brfalse.s  IL_0089

      .try
      {
        IL_0012:  ldarg.0
        IL_0013:  ldarg.1
        IL_0014:  ldarg.3
        IL_0015:  ldarg.s    description
        IL_0017:  ldarg.s    trace
        IL_0019:  ldarg.s    exception
        IL_001b:  ldarg.s    source
        IL_001d:  ldloca.s   V_0
        IL_001f:  call       instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::BuildTrace(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                             string,
                                                                                                             string,
                                                                                                             class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord,
                                                                                                             class [mscorlib]System.Exception,
                                                                                                             object,
                                                                                                             class System.ServiceModel.Diagnostics.TraceXPathNavigator&)
        IL_0024:  leave.s    IL_005d

      }  // end .try
      catch System.ServiceModel.Diagnostics.PlainXmlWriter/MaxSizeExceededException 
      {
        IL_0026:  pop
        IL_0027:  ldstr      "TruncatedTraceId"
        IL_002c:  ldarg.3
        IL_002d:  newobj     instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.StringTraceRecord::.ctor(string,
                                                                                                                              string)
        IL_0032:  stloc.1
        IL_0033:  ldarg.0
        IL_0034:  ldarg.1
        IL_0035:  ldc.i4     0x2000c
        IL_003a:  ldstr      "System.ServiceModel.Diagnostics"
        IL_003f:  ldstr      "TraceTruncatedQuotaExceeded"
        IL_0044:  call       string System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::GenerateMsdnTraceCode(string,
                                                                                                                 string)
        IL_0049:  ldstr      "TraceCodeTraceTruncatedQuotaExceeded"
        IL_004e:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
        IL_0053:  ldloc.1
        IL_0054:  ldnull
        IL_0055:  ldnull
        IL_0056:  call       instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::TraceEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                             int32,
                                                                                                             string,
                                                                                                             string,
                                                                                                             class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord,
                                                                                                             class [mscorlib]System.Exception,
                                                                                                             object)
        IL_005b:  leave.s    IL_005d

      }  // end handler
      IL_005d:  ldarg.0
      IL_005e:  call       instance class [System]System.Diagnostics.TraceSource [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_TraceSource()
      IL_0063:  ldarg.1
      IL_0064:  ldarg.2
      IL_0065:  ldloc.0
      IL_0066:  callvirt   instance void [System]System.Diagnostics.TraceSource::TraceData(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                           int32,
                                                                                           object)
      IL_006b:  ldarg.0
      IL_006c:  call       instance bool [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_CalledShutdown()
      IL_0071:  brfalse.s  IL_007e

      IL_0073:  ldarg.0
      IL_0074:  call       instance class [System]System.Diagnostics.TraceSource [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_TraceSource()
      IL_0079:  callvirt   instance void [System]System.Diagnostics.TraceSource::Flush()
      IL_007e:  ldarg.0
      IL_007f:  ldsfld     valuetype [mscorlib]System.DateTime [mscorlib]System.DateTime::MinValue
      IL_0084:  call       instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::set_LastFailure(valuetype [mscorlib]System.DateTime)
      IL_0089:  leave.s    IL_00af

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_008b:  stloc.2
      IL_008c:  ldloc.2
      IL_008d:  call       bool [System.ServiceModel.Internals]System.Runtime.Fx::IsFatal(class [mscorlib]System.Exception)
      IL_0092:  brfalse.s  IL_0096

      IL_0094:  rethrow
      IL_0096:  ldarg.0
      IL_0097:  ldloc.0
      IL_0098:  brfalse.s  IL_00a2

      IL_009a:  ldloc.0
      IL_009b:  callvirt   instance string [mscorlib]System.Object::ToString()
      IL_00a0:  br.s       IL_00a7

      IL_00a2:  ldsfld     string [mscorlib]System.String::Empty
      IL_00a7:  ldloc.2
      IL_00a8:  call       instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::LogTraceFailure(string,
                                                                                                                                        class [mscorlib]System.Exception)
      IL_00ad:  leave.s    IL_00af

    }  // end handler
    IL_00af:  ret
  } // end of method LegacyDiagnosticTrace::TraceEvent

  .method assembly hidebysig instance void 
          TraceEvent(valuetype [System]System.Diagnostics.TraceEventType 'type',
                     int32 code,
                     string msdnTraceCode,
                     string description,
                     class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord trace,
                     class [mscorlib]System.Exception exception,
                     valuetype [mscorlib]System.Guid activityId,
                     object source) cil managed
  {
    // Code size       63 (0x3f)
    .maxstack  8
    .locals init (class System.ServiceModel.Diagnostics.Activity V_0)
    IL_0000:  ldarg.0
    IL_0001:  call       instance bool System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::get_ShouldUseActivity()
    IL_0006:  brfalse.s  IL_0016

    IL_0008:  ldsfld     valuetype [mscorlib]System.Guid [mscorlib]System.Guid::Empty
    IL_000d:  ldarg.s    activityId
    IL_000f:  call       bool [mscorlib]System.Guid::op_Inequality(valuetype [mscorlib]System.Guid,
                                                                   valuetype [mscorlib]System.Guid)
    IL_0014:  brtrue.s   IL_0019

    IL_0016:  ldnull
    IL_0017:  br.s       IL_0020

    IL_0019:  ldarg.s    activityId
    IL_001b:  call       class System.ServiceModel.Diagnostics.Activity System.ServiceModel.Diagnostics.Activity::CreateActivity(valuetype [mscorlib]System.Guid)
    IL_0020:  stloc.0
    .try
    {
      IL_0021:  ldarg.0
      IL_0022:  ldarg.1
      IL_0023:  ldarg.2
      IL_0024:  ldarg.3
      IL_0025:  ldarg.s    description
      IL_0027:  ldarg.s    trace
      IL_0029:  ldarg.s    exception
      IL_002b:  ldarg.s    source
      IL_002d:  call       instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::TraceEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                           int32,
                                                                                                           string,
                                                                                                           string,
                                                                                                           class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord,
                                                                                                           class [mscorlib]System.Exception,
                                                                                                           object)
      IL_0032:  leave.s    IL_003e

    }  // end .try
    finally
    {
      IL_0034:  ldloc.0
      IL_0035:  brfalse.s  IL_003d

      IL_0037:  ldloc.0
      IL_0038:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_003d:  endfinally
    }  // end handler
    IL_003e:  ret
  } // end of method LegacyDiagnosticTrace::TraceEvent

  .method assembly hidebysig static string 
          GenerateMsdnTraceCode(string traceSource,
                                string traceCodeString) cil managed
  {
    // Code size       39 (0x27)
    .maxstack  8
    IL_0000:  call       class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_InvariantCulture()
    IL_0005:  ldstr      "https://docs.microsoft.com/dotnet/framework/wcf/di"
    + "agnostics/tracing/{0}-{1}"
    IL_000a:  ldc.i4.2
    IL_000b:  newarr     [mscorlib]System.Object
    IL_0010:  dup
    IL_0011:  ldc.i4.0
    IL_0012:  ldarg.0
    IL_0013:  ldc.i4.s   46
    IL_0015:  ldc.i4.s   45
    IL_0017:  callvirt   instance string [mscorlib]System.String::Replace(char,
                                                                          char)
    IL_001c:  stelem.ref
    IL_001d:  dup
    IL_001e:  ldc.i4.1
    IL_001f:  ldarg.1
    IL_0020:  stelem.ref
    IL_0021:  call       string [mscorlib]System.String::Format(class [mscorlib]System.IFormatProvider,
                                                                string,
                                                                object[])
    IL_0026:  ret
  } // end of method LegacyDiagnosticTrace::GenerateMsdnTraceCode

  .method assembly hidebysig instance void 
          TraceTransfer(valuetype [mscorlib]System.Guid newId) cil managed
  {
    // Code size       69 (0x45)
    .maxstack  4
    .locals init (valuetype [mscorlib]System.Guid V_0,
             class [mscorlib]System.Exception V_1)
    IL_0000:  ldarg.0
    IL_0001:  call       instance bool System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::get_ShouldUseActivity()
    IL_0006:  brfalse.s  IL_0044

    IL_0008:  call       valuetype [mscorlib]System.Guid [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_ActivityId()
    IL_000d:  stloc.0
    IL_000e:  ldarg.1
    IL_000f:  ldloc.0
    IL_0010:  call       bool [mscorlib]System.Guid::op_Inequality(valuetype [mscorlib]System.Guid,
                                                                   valuetype [mscorlib]System.Guid)
    IL_0015:  brfalse.s  IL_0044

    IL_0017:  ldarg.0
    IL_0018:  call       instance bool [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_HaveListeners()
    IL_001d:  brfalse.s  IL_0044

    .try
    {
      IL_001f:  ldarg.0
      IL_0020:  call       instance class [System]System.Diagnostics.TraceSource [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_TraceSource()
      IL_0025:  ldc.i4.0
      IL_0026:  ldnull
      IL_0027:  ldarg.1
      IL_0028:  callvirt   instance void [System]System.Diagnostics.TraceSource::TraceTransfer(int32,
                                                                                               string,
                                                                                               valuetype [mscorlib]System.Guid)
      IL_002d:  leave.s    IL_0044

    }  // end .try
    catch [mscorlib]System.Exception 
    {
      IL_002f:  stloc.1
      IL_0030:  ldloc.1
      IL_0031:  call       bool [System.ServiceModel.Internals]System.Runtime.Fx::IsFatal(class [mscorlib]System.Exception)
      IL_0036:  brfalse.s  IL_003a

      IL_0038:  rethrow
      IL_003a:  ldarg.0
      IL_003b:  ldnull
      IL_003c:  ldloc.1
      IL_003d:  call       instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::LogTraceFailure(string,
                                                                                                                                        class [mscorlib]System.Exception)
      IL_0042:  leave.s    IL_0044

    }  // end handler
    IL_0044:  ret
  } // end of method LegacyDiagnosticTrace::TraceTransfer

  .method family hidebysig virtual instance void 
          OnShutdownTracing() cil managed
  {
    // Code size       161 (0xa1)
    .maxstack  8
    .locals init (class [mscorlib]System.Collections.Generic.Dictionary`2<string,string> V_0,
             int32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  call       instance class [System]System.Diagnostics.TraceSource [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_TraceSource()
    IL_0006:  brfalse    IL_00a0

    IL_000b:  ldarg.0
    IL_000c:  call       instance valuetype [System]System.Diagnostics.SourceLevels [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_Level()
    IL_0011:  brfalse    IL_00a0

    IL_0016:  ldarg.0
    IL_0017:  ldc.i4.8
    IL_0018:  call       instance bool [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::ShouldTrace(valuetype [System]System.Diagnostics.TraceEventType)
    IL_001d:  brfalse.s  IL_0095

    IL_001f:  ldc.i4.3
    IL_0020:  newobj     instance void class [mscorlib]System.Collections.Generic.Dictionary`2<string,string>::.ctor(int32)
    IL_0025:  stloc.0
    IL_0026:  ldloc.0
    IL_0027:  ldstr      "AppDomain.FriendlyName"
    IL_002c:  call       class [mscorlib]System.AppDomain [mscorlib]System.AppDomain::get_CurrentDomain()
    IL_0031:  callvirt   instance string [mscorlib]System.AppDomain::get_FriendlyName()
    IL_0036:  callvirt   instance void class [mscorlib]System.Collections.Generic.Dictionary`2<string,string>::set_Item(!0,
                                                                                                                        !1)
    IL_003b:  ldloc.0
    IL_003c:  ldstr      "ProcessName"
    IL_0041:  call       string [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_ProcessName()
    IL_0046:  callvirt   instance void class [mscorlib]System.Collections.Generic.Dictionary`2<string,string>::set_Item(!0,
                                                                                                                        !1)
    IL_004b:  ldloc.0
    IL_004c:  ldstr      "ProcessId"
    IL_0051:  call       int32 [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_ProcessId()
    IL_0056:  stloc.1
    IL_0057:  ldloca.s   V_1
    IL_0059:  call       class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_CurrentCulture()
    IL_005e:  call       instance string [mscorlib]System.Int32::ToString(class [mscorlib]System.IFormatProvider)
    IL_0063:  callvirt   instance void class [mscorlib]System.Collections.Generic.Dictionary`2<string,string>::set_Item(!0,
                                                                                                                        !1)
    IL_0068:  ldarg.0
    IL_0069:  ldc.i4.8
    IL_006a:  ldc.i4     0x20001
    IL_006f:  ldstr      "System.ServiceModel.Diagnostics"
    IL_0074:  ldstr      "AppDomainUnload"
    IL_0079:  call       string System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::GenerateMsdnTraceCode(string,
                                                                                                             string)
    IL_007e:  ldstr      "TraceCodeAppDomainUnload"
    IL_0083:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
    IL_0088:  ldloc.0
    IL_0089:  newobj     instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.DictionaryTraceRecord::.ctor(class [mscorlib]System.Collections.IDictionary)
    IL_008e:  ldnull
    IL_008f:  ldnull
    IL_0090:  call       instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::TraceEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                         int32,
                                                                                                         string,
                                                                                                         string,
                                                                                                         class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord,
                                                                                                         class [mscorlib]System.Exception,
                                                                                                         object)
    IL_0095:  ldarg.0
    IL_0096:  call       instance class [System]System.Diagnostics.TraceSource [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_TraceSource()
    IL_009b:  callvirt   instance void [System]System.Diagnostics.TraceSource::Flush()
    IL_00a0:  ret
  } // end of method LegacyDiagnosticTrace::OnShutdownTracing

  .method family hidebysig virtual instance void 
          OnUnhandledException(class [mscorlib]System.Exception exception) cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  ldc.i4     0x20005
    IL_0007:  ldstr      "UnhandledException"
    IL_000c:  ldstr      "UnhandledException"
    IL_0011:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
    IL_0016:  ldnull
    IL_0017:  ldarg.1
    IL_0018:  ldnull
    IL_0019:  call       instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::TraceEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                         int32,
                                                                                                         string,
                                                                                                         string,
                                                                                                         class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord,
                                                                                                         class [mscorlib]System.Exception,
                                                                                                         object)
    IL_001e:  ret
  } // end of method LegacyDiagnosticTrace::OnUnhandledException

  .method public hidebysig specialname instance bool 
          get_ShouldLogPii() cil managed
  {
    // Code size       24 (0x18)
    .maxstack  1
    .locals init (class System.ServiceModel.Diagnostics.PiiTraceSource V_0)
    IL_0000:  ldarg.0
    IL_0001:  call       instance class [System]System.Diagnostics.TraceSource [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_TraceSource()
    IL_0006:  isinst     System.ServiceModel.Diagnostics.PiiTraceSource
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  brfalse.s  IL_0016

    IL_000f:  ldloc.0
    IL_0010:  callvirt   instance bool System.ServiceModel.Diagnostics.PiiTraceSource::get_ShouldLogPii()
    IL_0015:  ret

    IL_0016:  ldc.i4.0
    IL_0017:  ret
  } // end of method LegacyDiagnosticTrace::get_ShouldLogPii

  .method public hidebysig specialname instance void 
          set_ShouldLogPii(bool 'value') cil managed
  {
    // Code size       23 (0x17)
    .maxstack  2
    .locals init (class System.ServiceModel.Diagnostics.PiiTraceSource V_0)
    IL_0000:  ldarg.0
    IL_0001:  call       instance class [System]System.Diagnostics.TraceSource [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::get_TraceSource()
    IL_0006:  isinst     System.ServiceModel.Diagnostics.PiiTraceSource
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  brfalse.s  IL_0016

    IL_000f:  ldloc.0
    IL_0010:  ldarg.1
    IL_0011:  callvirt   instance void System.ServiceModel.Diagnostics.PiiTraceSource::set_ShouldLogPii(bool)
    IL_0016:  ret
  } // end of method LegacyDiagnosticTrace::set_ShouldLogPii

  .method private hidebysig instance void 
          BuildTrace(valuetype [System]System.Diagnostics.TraceEventType 'type',
                     string msdnTraceCode,
                     string description,
                     class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord trace,
                     class [mscorlib]System.Exception exception,
                     object source,
                     [out] class System.ServiceModel.Diagnostics.TraceXPathNavigator& navigator) cil managed
  {
    // Code size       58 (0x3a)
    .maxstack  8
    .locals init (class System.ServiceModel.Diagnostics.PlainXmlWriter V_0)
    IL_0000:  ldc.i4     0xffff
    IL_0005:  newobj     instance void System.ServiceModel.Diagnostics.PlainXmlWriter::.ctor(int32)
    IL_000a:  stloc.0
    IL_000b:  ldarg.s    navigator
    IL_000d:  ldloc.0
    IL_000e:  callvirt   instance class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::get_Navigator()
    IL_0013:  stind.ref
    IL_0014:  ldarg.0
    IL_0015:  ldloc.0
    IL_0016:  ldarg.1
    IL_0017:  ldarg.2
    IL_0018:  ldarg.3
    IL_0019:  ldarg.s    trace
    IL_001b:  ldarg.s    exception
    IL_001d:  ldarg.s    source
    IL_001f:  call       instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::BuildTrace(class System.ServiceModel.Diagnostics.PlainXmlWriter,
                                                                                                         valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                         string,
                                                                                                         string,
                                                                                                         class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord,
                                                                                                         class [mscorlib]System.Exception,
                                                                                                         object)
    IL_0024:  ldarg.0
    IL_0025:  call       instance bool System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::get_ShouldLogPii()
    IL_002a:  brtrue.s   IL_0039

    IL_002c:  ldarg.s    navigator
    IL_002e:  ldind.ref
    IL_002f:  ldsfld     string[][] System.ServiceModel.Diagnostics.DiagnosticStrings::HeadersPaths
    IL_0034:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::RemovePii(string[][])
    IL_0039:  ret
  } // end of method LegacyDiagnosticTrace::BuildTrace

  .method private hidebysig instance void 
          BuildTrace(class System.ServiceModel.Diagnostics.PlainXmlWriter xml,
                     valuetype [System]System.Diagnostics.TraceEventType 'type',
                     string msdnTraceCode,
                     string description,
                     class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord trace,
                     class [mscorlib]System.Exception exception,
                     object source) cil managed
  {
    // Code size       190 (0xbe)
    .maxstack  3
    IL_0000:  ldarg.1
    IL_0001:  ldstr      "TraceRecord"
    IL_0006:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteStartElement(string)
    IL_000b:  ldarg.1
    IL_000c:  ldstr      "xmlns"
    IL_0011:  ldstr      "http://schemas.microsoft.com/2004/10/E2ETraceEvent"
    + "/TraceRecord"
    IL_0016:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteAttributeString(string,
                                                                                              string)
    IL_001b:  ldarg.1
    IL_001c:  ldstr      "Severity"
    IL_0021:  ldarg.2
    IL_0022:  call       string [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::LookupSeverity(valuetype [System]System.Diagnostics.TraceEventType)
    IL_0027:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteAttributeString(string,
                                                                                              string)
    IL_002c:  ldarg.1
    IL_002d:  ldstr      "TraceIdentifier"
    IL_0032:  ldarg.3
    IL_0033:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteElementString(string,
                                                                                            string)
    IL_0038:  ldarg.1
    IL_0039:  ldstr      "Description"
    IL_003e:  ldarg.s    description
    IL_0040:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteElementString(string,
                                                                                            string)
    IL_0045:  ldarg.1
    IL_0046:  ldstr      "AppDomain"
    IL_004b:  ldsfld     string [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::AppDomainFriendlyName
    IL_0050:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteElementString(string,
                                                                                            string)
    IL_0055:  ldarg.s    source
    IL_0057:  brfalse.s  IL_006b

    IL_0059:  ldarg.1
    IL_005a:  ldstr      "Source"
    IL_005f:  ldarg.s    source
    IL_0061:  call       string [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::CreateSourceString(object)
    IL_0066:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteElementString(string,
                                                                                            string)
    IL_006b:  ldarg.s    trace
    IL_006d:  brfalse.s  IL_009a

    IL_006f:  ldarg.1
    IL_0070:  ldstr      "ExtendedData"
    IL_0075:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteStartElement(string)
    IL_007a:  ldarg.1
    IL_007b:  ldstr      "xmlns"
    IL_0080:  ldarg.s    trace
    IL_0082:  callvirt   instance string [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord::get_EventId()
    IL_0087:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteAttributeString(string,
                                                                                              string)
    IL_008c:  ldarg.s    trace
    IL_008e:  ldarg.1
    IL_008f:  callvirt   instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord::WriteTo(class [System.Xml]System.Xml.XmlWriter)
    IL_0094:  ldarg.1
    IL_0095:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteEndElement()
    IL_009a:  ldarg.s    exception
    IL_009c:  brfalse.s  IL_00b7

    IL_009e:  ldarg.1
    IL_009f:  ldstr      "Exception"
    IL_00a4:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteStartElement(string)
    IL_00a9:  ldarg.1
    IL_00aa:  ldarg.s    exception
    IL_00ac:  call       void [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase::AddExceptionToTraceString(class [System.Xml]System.Xml.XmlWriter,
                                                                                                                                       class [mscorlib]System.Exception)
    IL_00b1:  ldarg.1
    IL_00b2:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteEndElement()
    IL_00b7:  ldarg.1
    IL_00b8:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteEndElement()
    IL_00bd:  ret
  } // end of method LegacyDiagnosticTrace::BuildTrace

  .method public hidebysig virtual instance bool 
          IsEnabled() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method LegacyDiagnosticTrace::IsEnabled

  .method public hidebysig virtual instance void 
          TraceEventLogEvent(valuetype [System]System.Diagnostics.TraceEventType 'type',
                             class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord traceRecord) cil managed
  {
    // Code size       41 (0x29)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldc.i4     0x20002
    IL_0007:  ldstr      "System.ServiceModel.Diagnostics"
    IL_000c:  ldstr      "EventLog"
    IL_0011:  call       string System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::GenerateMsdnTraceCode(string,
                                                                                                             string)
    IL_0016:  ldstr      "TraceCodeEventLog"
    IL_001b:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
    IL_0020:  ldarg.2
    IL_0021:  ldnull
    IL_0022:  ldnull
    IL_0023:  call       instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::TraceEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                         int32,
                                                                                                         string,
                                                                                                         string,
                                                                                                         class [System.ServiceModel.Internals]System.Runtime.Diagnostics.TraceRecord,
                                                                                                         class [mscorlib]System.Exception,
                                                                                                         object)
    IL_0028:  ret
  } // end of method LegacyDiagnosticTrace::TraceEventLogEvent

  .method private hidebysig specialname rtspecialname static 
          void  .cctor() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0005:  stsfld     object System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::classLockObject
    IL_000a:  ret
  } // end of method LegacyDiagnosticTrace::.cctor

  .property instance bool ShouldUseActivity()
  {
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 50 46 6F 72 20 53 4D 44 69 61 67 6E 6F 73   // ..PFor SMDiagnos
                                                                                74 69 63 73 2E 64 6C 6C 20 75 73 65 20 6F 6E 6C   // tics.dll use onl
                                                                                79 2E 20 43 61 6C 6C 20 44 69 61 67 6E 6F 73 74   // y. Call Diagnost
                                                                                69 63 55 74 69 6C 69 74 79 2E 53 68 6F 75 6C 64   // icUtility.Should
                                                                                55 73 65 41 63 74 69 76 69 74 79 20 69 6E 73 74   // UseActivity inst
                                                                                65 61 64 00 00 )                                  // ead..
    .get instance bool System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::get_ShouldUseActivity()
  } // end of property LegacyDiagnosticTrace::ShouldUseActivity
  .property instance bool ShouldLogPii()
  {
    .get instance bool System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::get_ShouldLogPii()
    .set instance void System.ServiceModel.Diagnostics.LegacyDiagnosticTrace::set_ShouldLogPii(bool)
  } // end of property LegacyDiagnosticTrace::ShouldLogPii
} // end of class System.ServiceModel.Diagnostics.LegacyDiagnosticTrace

.class private abstract auto ansi sealed beforefieldinit System.ServiceModel.Diagnostics.NativeMethods
       extends [mscorlib]System.Object
{
  .field private static literal string ADVAPI32 = "advapi32.dll"
  .method assembly hidebysig static pinvokeimpl("advapi32.dll" unicode lasterr winapi) 
          class System.ServiceModel.Diagnostics.SafeEventLogWriteHandle 
          RegisterEventSource(string uncServerName,
                              string sourceName) cil managed preservesig
  {
    .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
  }
} // end of class System.ServiceModel.Diagnostics.NativeMethods

.class private auto ansi beforefieldinit System.ServiceModel.Diagnostics.PiiTraceSource
       extends [System]System.Diagnostics.TraceSource
{
  .field private string eventSourceName
  .field static assembly literal string LogPii = "logKnownPii"
  .field private bool shouldLogPii
  .field private bool initialized
  .field private object localSyncObject
  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(string name,
                               string eventSourceName) cil managed
  {
    // Code size       37 (0x25)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldsfld     string [mscorlib]System.String::Empty
    IL_0006:  stfld      string System.ServiceModel.Diagnostics.PiiTraceSource::eventSourceName
    IL_000b:  ldarg.0
    IL_000c:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0011:  stfld      object System.ServiceModel.Diagnostics.PiiTraceSource::localSyncObject
    IL_0016:  ldarg.0
    IL_0017:  ldarg.1
    IL_0018:  call       instance void [System]System.Diagnostics.TraceSource::.ctor(string)
    IL_001d:  ldarg.0
    IL_001e:  ldarg.2
    IL_001f:  stfld      string System.ServiceModel.Diagnostics.PiiTraceSource::eventSourceName
    IL_0024:  ret
  } // end of method PiiTraceSource::.ctor

  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(string name,
                               string eventSourceName,
                               valuetype [System]System.Diagnostics.SourceLevels levels) cil managed
  {
    // Code size       38 (0x26)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldsfld     string [mscorlib]System.String::Empty
    IL_0006:  stfld      string System.ServiceModel.Diagnostics.PiiTraceSource::eventSourceName
    IL_000b:  ldarg.0
    IL_000c:  newobj     instance void [mscorlib]System.Object::.ctor()
    IL_0011:  stfld      object System.ServiceModel.Diagnostics.PiiTraceSource::localSyncObject
    IL_0016:  ldarg.0
    IL_0017:  ldarg.1
    IL_0018:  ldarg.3
    IL_0019:  call       instance void [System]System.Diagnostics.TraceSource::.ctor(string,
                                                                                     valuetype [System]System.Diagnostics.SourceLevels)
    IL_001e:  ldarg.0
    IL_001f:  ldarg.2
    IL_0020:  stfld      string System.ServiceModel.Diagnostics.PiiTraceSource::eventSourceName
    IL_0025:  ret
  } // end of method PiiTraceSource::.ctor

  .method private hidebysig instance void 
          Initialize() cil managed
  {
    // Code size       170 (0xaa)
    .maxstack  6
    .locals init (object V_0,
             bool V_1,
             string V_2,
             bool V_3,
             class [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger V_4)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.PiiTraceSource::initialized
    IL_0006:  brtrue     IL_00a9

    IL_000b:  ldarg.0
    IL_000c:  ldfld      object System.ServiceModel.Diagnostics.PiiTraceSource::localSyncObject
    IL_0011:  stloc.0
    IL_0012:  ldc.i4.0
    IL_0013:  stloc.1
    .try
    {
      IL_0014:  ldloc.0
      IL_0015:  ldloca.s   V_1
      IL_0017:  call       void [mscorlib]System.Threading.Monitor::Enter(object,
                                                                          bool&)
      IL_001c:  ldarg.0
      IL_001d:  ldfld      bool System.ServiceModel.Diagnostics.PiiTraceSource::initialized
      IL_0022:  brtrue.s   IL_009d

      IL_0024:  ldarg.0
      IL_0025:  call       instance class [System]System.Collections.Specialized.StringDictionary [System]System.Diagnostics.TraceSource::get_Attributes()
      IL_002a:  ldstr      "logKnownPii"
      IL_002f:  callvirt   instance string [System]System.Collections.Specialized.StringDictionary::get_Item(string)
      IL_0034:  stloc.2
      IL_0035:  ldc.i4.0
      IL_0036:  stloc.3
      IL_0037:  ldloc.2
      IL_0038:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
      IL_003d:  brtrue.s   IL_004b

      IL_003f:  ldloc.2
      IL_0040:  ldloca.s   V_3
      IL_0042:  call       bool [mscorlib]System.Boolean::TryParse(string,
                                                                   bool&)
      IL_0047:  brtrue.s   IL_004b

      IL_0049:  ldc.i4.0
      IL_004a:  stloc.3
      IL_004b:  ldloc.3
      IL_004c:  brfalse.s  IL_0096

      IL_004e:  ldarg.0
      IL_004f:  ldfld      string System.ServiceModel.Diagnostics.PiiTraceSource::eventSourceName
      IL_0054:  ldnull
      IL_0055:  newobj     instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::.ctor(string,
                                                                                                                      class [System.ServiceModel.Internals]System.Runtime.Diagnostics.DiagnosticTraceBase)
      IL_005a:  stloc.s    V_4
      IL_005c:  call       bool System.ServiceModel.Configuration.MachineSettingsSection::get_EnableLoggingKnownPii()
      IL_0061:  brfalse.s  IL_0081

      IL_0063:  ldloc.s    V_4
      IL_0065:  ldc.i4.8
      IL_0066:  ldc.i4.7
      IL_0067:  ldc.i4     0xc001006b
      IL_006c:  ldc.i4.0
      IL_006d:  ldc.i4.0
      IL_006e:  newarr     [mscorlib]System.String
      IL_0073:  callvirt   instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::LogEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                                         uint16,
                                                                                                                         uint32,
                                                                                                                         bool,
                                                                                                                         string[])
      IL_0078:  ldarg.0
      IL_0079:  ldc.i4.1
      IL_007a:  stfld      bool System.ServiceModel.Diagnostics.PiiTraceSource::shouldLogPii
      IL_007f:  br.s       IL_0096

      IL_0081:  ldloc.s    V_4
      IL_0083:  ldc.i4.2
      IL_0084:  ldc.i4.7
      IL_0085:  ldc.i4     0xc001006c
      IL_008a:  ldc.i4.0
      IL_008b:  ldc.i4.0
      IL_008c:  newarr     [mscorlib]System.String
      IL_0091:  callvirt   instance void [System.ServiceModel.Internals]System.Runtime.Diagnostics.EventLogger::LogEvent(valuetype [System]System.Diagnostics.TraceEventType,
                                                                                                                         uint16,
                                                                                                                         uint32,
                                                                                                                         bool,
                                                                                                                         string[])
      IL_0096:  ldarg.0
      IL_0097:  ldc.i4.1
      IL_0098:  stfld      bool System.ServiceModel.Diagnostics.PiiTraceSource::initialized
      IL_009d:  leave.s    IL_00a9

    }  // end .try
    finally
    {
      IL_009f:  ldloc.1
      IL_00a0:  brfalse.s  IL_00a8

      IL_00a2:  ldloc.0
      IL_00a3:  call       void [mscorlib]System.Threading.Monitor::Exit(object)
      IL_00a8:  endfinally
    }  // end handler
    IL_00a9:  ret
  } // end of method PiiTraceSource::Initialize

  .method family hidebysig virtual instance string[] 
          GetSupportedAttributes() cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  newarr     [mscorlib]System.String
    IL_0006:  dup
    IL_0007:  ldc.i4.0
    IL_0008:  ldstr      "logKnownPii"
    IL_000d:  stelem.ref
    IL_000e:  ret
  } // end of method PiiTraceSource::GetSupportedAttributes

  .method assembly hidebysig specialname 
          instance bool  get_ShouldLogPii() cil managed
  {
    // Code size       21 (0x15)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.PiiTraceSource::initialized
    IL_0006:  brtrue.s   IL_000e

    IL_0008:  ldarg.0
    IL_0009:  call       instance void System.ServiceModel.Diagnostics.PiiTraceSource::Initialize()
    IL_000e:  ldarg.0
    IL_000f:  ldfld      bool System.ServiceModel.Diagnostics.PiiTraceSource::shouldLogPii
    IL_0014:  ret
  } // end of method PiiTraceSource::get_ShouldLogPii

  .method assembly hidebysig specialname 
          instance void  set_ShouldLogPii(bool 'value') cil managed
  {
    // Code size       15 (0xf)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  stfld      bool System.ServiceModel.Diagnostics.PiiTraceSource::initialized
    IL_0007:  ldarg.0
    IL_0008:  ldarg.1
    IL_0009:  stfld      bool System.ServiceModel.Diagnostics.PiiTraceSource::shouldLogPii
    IL_000e:  ret
  } // end of method PiiTraceSource::set_ShouldLogPii

  .property instance bool ShouldLogPii()
  {
    .get instance bool System.ServiceModel.Diagnostics.PiiTraceSource::get_ShouldLogPii()
    .set instance void System.ServiceModel.Diagnostics.PiiTraceSource::set_ShouldLogPii(bool)
  } // end of property PiiTraceSource::ShouldLogPii
} // end of class System.ServiceModel.Diagnostics.PiiTraceSource

.class private auto ansi beforefieldinit System.ServiceModel.Diagnostics.PlainXmlWriter
       extends [System.Xml]System.Xml.XmlWriter
{
  .class auto ansi nested assembly beforefieldinit MaxSizeExceededException
         extends [mscorlib]System.Exception
  {
    .method public hidebysig specialname rtspecialname 
            instance void  .ctor() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Exception::.ctor()
      IL_0006:  ret
    } // end of method MaxSizeExceededException::.ctor

  } // end of class MaxSizeExceededException

  .field private class System.ServiceModel.Diagnostics.TraceXPathNavigator navigator
  .field private bool writingAttribute
  .field private string currentAttributeName
  .field private string currentAttributePrefix
  .field private string currentAttributeNs
  .field private string currentAttributeText
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.m1
    IL_0002:  call       instance void System.ServiceModel.Diagnostics.PlainXmlWriter::.ctor(int32)
    IL_0007:  ret
  } // end of method PlainXmlWriter::.ctor

  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 maxSize) cil managed
  {
    // Code size       30 (0x1e)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldsfld     string [mscorlib]System.String::Empty
    IL_0006:  stfld      string System.ServiceModel.Diagnostics.PlainXmlWriter::currentAttributeText
    IL_000b:  ldarg.0
    IL_000c:  call       instance void [System.Xml]System.Xml.XmlWriter::.ctor()
    IL_0011:  ldarg.0
    IL_0012:  ldarg.1
    IL_0013:  newobj     instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::.ctor(int32)
    IL_0018:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::navigator
    IL_001d:  ret
  } // end of method PlainXmlWriter::.ctor

  .method public hidebysig specialname instance class System.ServiceModel.Diagnostics.TraceXPathNavigator 
          get_Navigator() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::navigator
    IL_0006:  ret
  } // end of method PlainXmlWriter::get_Navigator

  .method public hidebysig virtual instance void 
          WriteStartDocument() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PlainXmlWriter::WriteStartDocument

  .method public hidebysig virtual instance void 
          WriteStartDocument(bool standalone) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PlainXmlWriter::WriteStartDocument

  .method public hidebysig virtual instance void 
          WriteDocType(string name,
                       string pubid,
                       string sysid,
                       string subset) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PlainXmlWriter::WriteDocType

  .method public hidebysig virtual instance void 
          WriteEndDocument() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PlainXmlWriter::WriteEndDocument

  .method public hidebysig virtual instance string 
          LookupPrefix(string ns) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::navigator
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string [System.Xml]System.Xml.XPath.XPathNavigator::LookupPrefix(string)
    IL_000c:  ret
  } // end of method PlainXmlWriter::LookupPrefix

  .method public hidebysig specialname virtual 
          instance valuetype [System.Xml]System.Xml.WriteState 
          get_WriteState() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::navigator
    IL_0006:  callvirt   instance valuetype [System.Xml]System.Xml.WriteState System.ServiceModel.Diagnostics.TraceXPathNavigator::get_WriteState()
    IL_000b:  ret
  } // end of method PlainXmlWriter::get_WriteState

  .method public hidebysig specialname virtual 
          instance valuetype [System.Xml]System.Xml.XmlSpace 
          get_XmlSpace() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.1
    IL_0001:  ret
  } // end of method PlainXmlWriter::get_XmlSpace

  .method public hidebysig specialname virtual 
          instance string  get_XmlLang() cil managed
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  ldsfld     string [mscorlib]System.String::Empty
    IL_0005:  ret
  } // end of method PlainXmlWriter::get_XmlLang

  .method public hidebysig virtual instance void 
          WriteValue(object 'value') cil managed
  {
    // Code size       18 (0x12)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::navigator
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_000c:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::AddText(string)
    IL_0011:  ret
  } // end of method PlainXmlWriter::WriteValue

  .method public hidebysig virtual instance void 
          WriteValue(string 'value') cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::navigator
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::AddText(string)
    IL_000c:  ret
  } // end of method PlainXmlWriter::WriteValue

  .method public hidebysig virtual instance void 
          WriteBase64(uint8[] buffer,
                      int32 offset,
                      int32 count) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PlainXmlWriter::WriteBase64

  .method public hidebysig virtual instance void 
          WriteStartElement(string prefix,
                            string localName,
                            string ns) cil managed
  {
    // Code size       34 (0x22)
    .maxstack  8
    IL_0000:  ldarg.2
    IL_0001:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
    IL_0006:  brfalse.s  IL_0013

    IL_0008:  ldstr      "localName"
    IL_000d:  newobj     instance void [mscorlib]System.ArgumentNullException::.ctor(string)
    IL_0012:  throw

    IL_0013:  ldarg.0
    IL_0014:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::navigator
    IL_0019:  ldarg.1
    IL_001a:  ldarg.2
    IL_001b:  ldarg.3
    IL_001c:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::AddElement(string,
                                                                                                       string,
                                                                                                       string)
    IL_0021:  ret
  } // end of method PlainXmlWriter::WriteStartElement

  .method public hidebysig virtual instance void 
          WriteFullEndElement() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteEndElement()
    IL_0006:  ret
  } // end of method PlainXmlWriter::WriteFullEndElement

  .method public hidebysig virtual instance void 
          WriteEndElement() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::navigator
    IL_0006:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::CloseElement()
    IL_000b:  ret
  } // end of method PlainXmlWriter::WriteEndElement

  .method public hidebysig virtual instance void 
          WriteStartAttribute(string prefix,
                              string localName,
                              string ns) cil managed
  {
    // Code size       54 (0x36)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.PlainXmlWriter::writingAttribute
    IL_0006:  brfalse.s  IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldarg.0
    IL_000f:  ldarg.2
    IL_0010:  stfld      string System.ServiceModel.Diagnostics.PlainXmlWriter::currentAttributeName
    IL_0015:  ldarg.0
    IL_0016:  ldarg.1
    IL_0017:  stfld      string System.ServiceModel.Diagnostics.PlainXmlWriter::currentAttributePrefix
    IL_001c:  ldarg.0
    IL_001d:  ldarg.3
    IL_001e:  stfld      string System.ServiceModel.Diagnostics.PlainXmlWriter::currentAttributeNs
    IL_0023:  ldarg.0
    IL_0024:  ldsfld     string [mscorlib]System.String::Empty
    IL_0029:  stfld      string System.ServiceModel.Diagnostics.PlainXmlWriter::currentAttributeText
    IL_002e:  ldarg.0
    IL_002f:  ldc.i4.1
    IL_0030:  stfld      bool System.ServiceModel.Diagnostics.PlainXmlWriter::writingAttribute
    IL_0035:  ret
  } // end of method PlainXmlWriter::WriteStartAttribute

  .method public hidebysig virtual instance void 
          WriteEndAttribute() cil managed
  {
    // Code size       57 (0x39)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.PlainXmlWriter::writingAttribute
    IL_0006:  brtrue.s   IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldarg.0
    IL_000f:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::navigator
    IL_0014:  ldarg.0
    IL_0015:  ldfld      string System.ServiceModel.Diagnostics.PlainXmlWriter::currentAttributeName
    IL_001a:  ldarg.0
    IL_001b:  ldfld      string System.ServiceModel.Diagnostics.PlainXmlWriter::currentAttributeText
    IL_0020:  ldarg.0
    IL_0021:  ldfld      string System.ServiceModel.Diagnostics.PlainXmlWriter::currentAttributeNs
    IL_0026:  ldarg.0
    IL_0027:  ldfld      string System.ServiceModel.Diagnostics.PlainXmlWriter::currentAttributePrefix
    IL_002c:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::AddAttribute(string,
                                                                                                         string,
                                                                                                         string,
                                                                                                         string)
    IL_0031:  ldarg.0
    IL_0032:  ldc.i4.0
    IL_0033:  stfld      bool System.ServiceModel.Diagnostics.PlainXmlWriter::writingAttribute
    IL_0038:  ret
  } // end of method PlainXmlWriter::WriteEndAttribute

  .method public hidebysig virtual instance void 
          WriteCData(string text) cil managed
  {
    // Code size       23 (0x17)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldstr      "<![CDATA["
    IL_0006:  ldarg.1
    IL_0007:  ldstr      "]]>"
    IL_000c:  call       string [mscorlib]System.String::Concat(string,
                                                                string,
                                                                string)
    IL_0011:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteRaw(string)
    IL_0016:  ret
  } // end of method PlainXmlWriter::WriteCData

  .method public hidebysig virtual instance void 
          WriteComment(string text) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::navigator
    IL_0006:  ldarg.1
    IL_0007:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::AddComment(string)
    IL_000c:  ret
  } // end of method PlainXmlWriter::WriteComment

  .method public hidebysig virtual instance void 
          WriteProcessingInstruction(string name,
                                     string text) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::navigator
    IL_0006:  ldarg.1
    IL_0007:  ldarg.2
    IL_0008:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::AddProcessingInstruction(string,
                                                                                                                     string)
    IL_000d:  ret
  } // end of method PlainXmlWriter::WriteProcessingInstruction

  .method public hidebysig virtual instance void 
          WriteEntityRef(string name) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PlainXmlWriter::WriteEntityRef

  .method public hidebysig virtual instance void 
          WriteCharEntity(char ch) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PlainXmlWriter::WriteCharEntity

  .method public hidebysig virtual instance void 
          WriteSurrogateCharEntity(char lowChar,
                                   char highChar) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PlainXmlWriter::WriteSurrogateCharEntity

  .method public hidebysig virtual instance void 
          WriteWhitespace(string ws) cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PlainXmlWriter::WriteWhitespace

  .method public hidebysig virtual instance void 
          WriteString(string text) cil managed
  {
    // Code size       35 (0x23)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.PlainXmlWriter::writingAttribute
    IL_0006:  brfalse.s  IL_001b

    IL_0008:  ldarg.0
    IL_0009:  ldarg.0
    IL_000a:  ldfld      string System.ServiceModel.Diagnostics.PlainXmlWriter::currentAttributeText
    IL_000f:  ldarg.1
    IL_0010:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0015:  stfld      string System.ServiceModel.Diagnostics.PlainXmlWriter::currentAttributeText
    IL_001a:  ret

    IL_001b:  ldarg.0
    IL_001c:  ldarg.1
    IL_001d:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteValue(string)
    IL_0022:  ret
  } // end of method PlainXmlWriter::WriteString

  .method public hidebysig virtual instance void 
          WriteChars(char[] buffer,
                     int32 index,
                     int32 count) cil managed
  {
    // Code size       83 (0x53)
    .maxstack  4
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000e

    IL_0003:  ldstr      "buffer"
    IL_0008:  newobj     instance void [mscorlib]System.ArgumentNullException::.ctor(string)
    IL_000d:  throw

    IL_000e:  ldarg.2
    IL_000f:  ldc.i4.0
    IL_0010:  bge.s      IL_001d

    IL_0012:  ldstr      "index"
    IL_0017:  newobj     instance void [mscorlib]System.ArgumentOutOfRangeException::.ctor(string)
    IL_001c:  throw

    IL_001d:  ldarg.3
    IL_001e:  ldc.i4.0
    IL_001f:  bge.s      IL_002c

    IL_0021:  ldstr      "count"
    IL_0026:  newobj     instance void [mscorlib]System.ArgumentOutOfRangeException::.ctor(string)
    IL_002b:  throw

    IL_002c:  ldarg.1
    IL_002d:  ldlen
    IL_002e:  conv.i4
    IL_002f:  ldarg.2
    IL_0030:  sub
    IL_0031:  ldarg.3
    IL_0032:  bge.s      IL_0044

    IL_0034:  ldstr      "WriteCharsInvalidContent"
    IL_0039:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
    IL_003e:  newobj     instance void [mscorlib]System.ArgumentException::.ctor(string)
    IL_0043:  throw

    IL_0044:  ldarg.0
    IL_0045:  ldarg.1
    IL_0046:  ldarg.2
    IL_0047:  ldarg.3
    IL_0048:  newobj     instance void [mscorlib]System.String::.ctor(char[],
                                                                      int32,
                                                                      int32)
    IL_004d:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteString(string)
    IL_0052:  ret
  } // end of method PlainXmlWriter::WriteChars

  .method public hidebysig virtual instance void 
          WriteRaw(string data) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteString(string)
    IL_0007:  ret
  } // end of method PlainXmlWriter::WriteRaw

  .method public hidebysig virtual instance void 
          WriteRaw(char[] buffer,
                   int32 index,
                   int32 count) cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.2
    IL_0003:  ldarg.3
    IL_0004:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteChars(char[],
                                                                                    int32,
                                                                                    int32)
    IL_0009:  ret
  } // end of method PlainXmlWriter::WriteRaw

  .method public hidebysig virtual instance void 
          Close() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PlainXmlWriter::Close

  .method public hidebysig virtual instance void 
          Flush() cil managed
  {
    // Code size       1 (0x1)
    .maxstack  8
    IL_0000:  ret
  } // end of method PlainXmlWriter::Flush

  .property instance class System.ServiceModel.Diagnostics.TraceXPathNavigator
          Navigator()
  {
    .get instance class System.ServiceModel.Diagnostics.TraceXPathNavigator System.ServiceModel.Diagnostics.PlainXmlWriter::get_Navigator()
  } // end of property PlainXmlWriter::Navigator
  .property instance valuetype [System.Xml]System.Xml.WriteState
          WriteState()
  {
    .get instance valuetype [System.Xml]System.Xml.WriteState System.ServiceModel.Diagnostics.PlainXmlWriter::get_WriteState()
  } // end of property PlainXmlWriter::WriteState
  .property instance valuetype [System.Xml]System.Xml.XmlSpace
          XmlSpace()
  {
    .get instance valuetype [System.Xml]System.Xml.XmlSpace System.ServiceModel.Diagnostics.PlainXmlWriter::get_XmlSpace()
  } // end of property PlainXmlWriter::XmlSpace
  .property instance string XmlLang()
  {
    .get instance string System.ServiceModel.Diagnostics.PlainXmlWriter::get_XmlLang()
  } // end of property PlainXmlWriter::XmlLang
} // end of class System.ServiceModel.Diagnostics.PlainXmlWriter

.class private auto ansi sealed beforefieldinit System.ServiceModel.Diagnostics.SafeEventLogWriteHandle
       extends [mscorlib]Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid
{
  .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
  .method private hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  call       instance void [mscorlib]Microsoft.Win32.SafeHandles.SafeHandleZeroOrMinusOneIsInvalid::.ctor(bool)
    IL_0007:  ret
  } // end of method SafeEventLogWriteHandle::.ctor

  .method assembly hidebysig static class System.ServiceModel.Diagnostics.SafeEventLogWriteHandle 
          RegisterEventSource(string uncServerName,
                              string sourceName) cil managed
  {
    .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       23 (0x17)
    .maxstack  2
    .locals init (class System.ServiceModel.Diagnostics.SafeEventLogWriteHandle V_0,
             int32 V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       class System.ServiceModel.Diagnostics.SafeEventLogWriteHandle System.ServiceModel.Diagnostics.NativeMethods::RegisterEventSource(string,
                                                                                                                                                          string)
    IL_0007:  stloc.0
    IL_0008:  call       int32 [mscorlib]System.Runtime.InteropServices.Marshal::GetLastWin32Error()
    IL_000d:  stloc.1
    IL_000e:  ldloc.0
    IL_000f:  callvirt   instance bool [mscorlib]System.Runtime.InteropServices.SafeHandle::get_IsInvalid()
    IL_0014:  pop
    IL_0015:  ldloc.0
    IL_0016:  ret
  } // end of method SafeEventLogWriteHandle::RegisterEventSource

  .method private hidebysig static pinvokeimpl("advapi32" lasterr winapi) 
          bool  DeregisterEventSource(native int hEventLog) cil managed preservesig
  {
  }
  .method family hidebysig virtual instance bool 
          ReleaseHandle() cil managed
  {
    .custom instance void [mscorlib]System.Security.SecurityCriticalAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      native int [mscorlib]System.Runtime.InteropServices.SafeHandle::handle
    IL_0006:  call       bool System.ServiceModel.Diagnostics.SafeEventLogWriteHandle::DeregisterEventSource(native int)
    IL_000b:  ret
  } // end of method SafeEventLogWriteHandle::ReleaseHandle

} // end of class System.ServiceModel.Diagnostics.SafeEventLogWriteHandle

.class private abstract auto ansi sealed beforefieldinit System.ServiceModel.Diagnostics.DiagnosticsTraceCode
       extends [mscorlib]System.Object
{
  .field public static literal int32 Diagnostics = int32(0x00020000)
  .field public static literal int32 AppDomainUnload = int32(0x00020001)
  .field public static literal int32 EventLog = int32(0x00020002)
  .field public static literal int32 ThrowingException = int32(0x00020003)
  .field public static literal int32 TraceHandledException = int32(0x00020004)
  .field public static literal int32 UnhandledException = int32(0x00020005)
  .field public static literal int32 TraceTruncatedQuotaExceeded = int32(0x0002000C)
} // end of class System.ServiceModel.Diagnostics.DiagnosticsTraceCode

.class private auto ansi sealed System.ServiceModel.Diagnostics.TraceSourceKind
       extends [mscorlib]System.Enum
{
  .field public specialname rtspecialname int32 value__
  .field public static literal valuetype System.ServiceModel.Diagnostics.TraceSourceKind DiagnosticTraceSource = int32(0x00000000)
  .field public static literal valuetype System.ServiceModel.Diagnostics.TraceSourceKind PiiTraceSource = int32(0x00000001)
} // end of class System.ServiceModel.Diagnostics.TraceSourceKind

.class private auto ansi beforefieldinit System.ServiceModel.Diagnostics.TraceXPathNavigator
       extends [System.Xml]System.Xml.XPath.XPathNavigator
{
  .custom instance void [mscorlib]System.Diagnostics.DebuggerDisplayAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
  .class interface abstract auto ansi nested private IMeasurable
  {
    .method public hidebysig newslot specialname abstract virtual 
            instance int32  get_Size() cil managed
    {
    } // end of method IMeasurable::get_Size

    .property instance int32 Size()
    {
      .get instance int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable::get_Size()
    } // end of property IMeasurable::Size
  } // end of class IMeasurable

  .class auto ansi nested private beforefieldinit TraceNode
         extends [mscorlib]System.Object
  {
    .field private valuetype [System.Xml]System.Xml.XPath.XPathNodeType nodeType
    .field assembly class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode parent
    .method family hidebysig specialname rtspecialname 
            instance void  .ctor(valuetype [System.Xml]System.Xml.XPath.XPathNodeType nodeType,
                                 class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode parent) cil managed
    {
      // Code size       21 (0x15)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::nodeType
      IL_000d:  ldarg.0
      IL_000e:  ldarg.2
      IL_000f:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::parent
      IL_0014:  ret
    } // end of method TraceNode::.ctor

    .method assembly hidebysig specialname 
            instance valuetype [System.Xml]System.Xml.XPath.XPathNodeType 
            get_NodeType() cil managed
    {
      // Code size       7 (0x7)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::nodeType
      IL_0006:  ret
    } // end of method TraceNode::get_NodeType

    .property instance valuetype [System.Xml]System.Xml.XPath.XPathNodeType
            NodeType()
    {
      .get instance valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::get_NodeType()
    } // end of property TraceNode::NodeType
  } // end of class TraceNode

  .class auto ansi nested private beforefieldinit CommentNode
         extends System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode
         implements System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable
  {
    .field assembly string nodeValue
    .method assembly hidebysig specialname rtspecialname 
            instance void  .ctor(string text,
                                 class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode parent) cil managed
    {
      // Code size       16 (0x10)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.8
      IL_0002:  ldarg.2
      IL_0003:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::.ctor(valuetype [System.Xml]System.Xml.XPath.XPathNodeType,
                                                                                                              class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode)
      IL_0008:  ldarg.0
      IL_0009:  ldarg.1
      IL_000a:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode::nodeValue
      IL_000f:  ret
    } // end of method CommentNode::.ctor

    .method public hidebysig newslot specialname virtual final 
            instance int32  get_Size() cil managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode::nodeValue
      IL_0006:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_000b:  ldc.i4.8
      IL_000c:  add
      IL_000d:  ret
    } // end of method CommentNode::get_Size

    .property instance int32 Size()
    {
      .get instance int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode::get_Size()
    } // end of property CommentNode::Size
  } // end of class CommentNode

  .class auto ansi nested private beforefieldinit ElementNode
         extends System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode
         implements System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable
  {
    .class auto ansi sealed nested private beforefieldinit '<FindSubnodes>d__11'
           extends [mscorlib]System.Object
           implements class [mscorlib]System.Collections.Generic.IEnumerable`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode>,
                      [mscorlib]System.Collections.IEnumerable,
                      class [mscorlib]System.Collections.Generic.IEnumerator`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode>,
                      [mscorlib]System.IDisposable,
                      [mscorlib]System.Collections.IEnumerator
    {
      .custom instance void [mscorlib]System.Runtime.CompilerServices.CompilerGeneratedAttribute::.ctor() = ( 01 00 00 00 ) 
      .field private int32 '<>1__state'
      .field private class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode '<>2__current'
      .field private int32 '<>l__initialThreadId'
      .field private string[] headersPath
      .field public string[] '<>3__headersPath'
      .field public class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode '<>4__this'
      .field private int32 '<i>5__2'
      .field private class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode '<subNode>5__3'
      .field private valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> '<>7__wrap3'
      .method public hidebysig specialname rtspecialname 
              instance void  .ctor(int32 '<>1__state') cil managed
      {
        .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
        // Code size       25 (0x19)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
        IL_0006:  ldarg.0
        IL_0007:  ldarg.1
        IL_0008:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>1__state'
        IL_000d:  ldarg.0
        IL_000e:  call       int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
        IL_0013:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>l__initialThreadId'
        IL_0018:  ret
      } // end of method '<FindSubnodes>d__11'::.ctor

      .method private hidebysig newslot virtual final 
              instance void  System.IDisposable.Dispose() cil managed
      {
        .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
        .override [mscorlib]System.IDisposable::Dispose
        // Code size       27 (0x1b)
        .maxstack  2
        .locals init (int32 V_0)
        IL_0000:  ldarg.0
        IL_0001:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>1__state'
        IL_0006:  stloc.0
        IL_0007:  ldloc.0
        IL_0008:  ldc.i4.s   -3
        IL_000a:  beq.s      IL_0010

        IL_000c:  ldloc.0
        IL_000d:  ldc.i4.1
        IL_000e:  bne.un.s   IL_001a

        IL_0010:  nop
        .try
        {
          IL_0011:  leave.s    IL_001a

        }  // end .try
        finally
        {
          IL_0013:  ldarg.0
          IL_0014:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>m__Finally1'()
          IL_0019:  endfinally
        }  // end handler
        IL_001a:  ret
      } // end of method '<FindSubnodes>d__11'::System.IDisposable.Dispose

      .method private hidebysig newslot virtual final 
              instance bool  MoveNext() cil managed
      {
        .override [mscorlib]System.Collections.IEnumerator::MoveNext
        // Code size       356 (0x164)
        .maxstack  3
        .locals init (bool V_0,
                 int32 V_1,
                 class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode V_2,
                 class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode V_3,
                 class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode V_4,
                 class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode V_5,
                 int32 V_6)
        .try
        {
          IL_0000:  ldarg.0
          IL_0001:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>1__state'
          IL_0006:  stloc.1
          IL_0007:  ldarg.0
          IL_0008:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>4__this'
          IL_000d:  stloc.2
          IL_000e:  ldloc.1
          IL_000f:  brfalse.s  IL_001f

          IL_0011:  ldloc.1
          IL_0012:  ldc.i4.1
          IL_0013:  beq        IL_00ef

          IL_0018:  ldc.i4.0
          IL_0019:  stloc.0
          IL_001a:  leave      IL_0162

          IL_001f:  ldarg.0
          IL_0020:  ldc.i4.m1
          IL_0021:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>1__state'
          IL_0026:  ldarg.0
          IL_0027:  ldfld      string[] System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::headersPath
          IL_002c:  brtrue.s   IL_0039

          IL_002e:  ldstr      "headersPath"
          IL_0033:  newobj     instance void [mscorlib]System.ArgumentNullException::.ctor(string)
          IL_0038:  throw

          IL_0039:  ldloc.2
          IL_003a:  stloc.3
          IL_003b:  ldloc.3
          IL_003c:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::name
          IL_0041:  ldarg.0
          IL_0042:  ldfld      string[] System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::headersPath
          IL_0047:  ldc.i4.0
          IL_0048:  ldelem.ref
          IL_0049:  call       int32 [mscorlib]System.String::CompareOrdinal(string,
                                                                             string)
          IL_004e:  brfalse.s  IL_0052

          IL_0050:  ldnull
          IL_0051:  stloc.3
          IL_0052:  ldarg.0
          IL_0053:  ldc.i4.0
          IL_0054:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<i>5__2'
          IL_0059:  br         IL_0133

          IL_005e:  ldarg.0
          IL_005f:  ldnull
          IL_0060:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<subNode>5__3'
          IL_0065:  ldloc.3
          IL_0066:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
          IL_006b:  brfalse    IL_0125

          IL_0070:  ldarg.0
          IL_0071:  ldloc.3
          IL_0072:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
          IL_0077:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::GetEnumerator()
          IL_007c:  stfld      valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>7__wrap3'
          IL_0081:  ldarg.0
          IL_0082:  ldc.i4.s   -3
          IL_0084:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>1__state'
          IL_0089:  br.s       IL_0103

          IL_008b:  ldarg.0
          IL_008c:  ldflda     valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>7__wrap3'
          IL_0091:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::get_Current()
          IL_0096:  stloc.s    V_4
          IL_0098:  ldloc.s    V_4
          IL_009a:  callvirt   instance valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::get_NodeType()
          IL_009f:  ldc.i4.1
          IL_00a0:  bne.un.s   IL_0103

          IL_00a2:  ldloc.s    V_4
          IL_00a4:  isinst     System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode
          IL_00a9:  stloc.s    V_5
          IL_00ab:  ldloc.s    V_5
          IL_00ad:  brfalse.s  IL_0103

          IL_00af:  ldloc.s    V_5
          IL_00b1:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::name
          IL_00b6:  ldarg.0
          IL_00b7:  ldfld      string[] System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::headersPath
          IL_00bc:  ldarg.0
          IL_00bd:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<i>5__2'
          IL_00c2:  ldelem.ref
          IL_00c3:  call       int32 [mscorlib]System.String::CompareOrdinal(string,
                                                                             string)
          IL_00c8:  brtrue.s   IL_0103

          IL_00ca:  ldarg.0
          IL_00cb:  ldfld      string[] System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::headersPath
          IL_00d0:  ldlen
          IL_00d1:  conv.i4
          IL_00d2:  ldarg.0
          IL_00d3:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<i>5__2'
          IL_00d8:  ldc.i4.1
          IL_00d9:  add
          IL_00da:  bne.un.s   IL_00f9

          IL_00dc:  ldarg.0
          IL_00dd:  ldloc.s    V_5
          IL_00df:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>2__current'
          IL_00e4:  ldarg.0
          IL_00e5:  ldc.i4.1
          IL_00e6:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>1__state'
          IL_00eb:  ldc.i4.1
          IL_00ec:  stloc.0
          IL_00ed:  leave.s    IL_0162

          IL_00ef:  ldarg.0
          IL_00f0:  ldc.i4.s   -3
          IL_00f2:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>1__state'
          IL_00f7:  br.s       IL_0103

          IL_00f9:  ldarg.0
          IL_00fa:  ldloc.s    V_5
          IL_00fc:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<subNode>5__3'
          IL_0101:  br.s       IL_0113

          IL_0103:  ldarg.0
          IL_0104:  ldflda     valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>7__wrap3'
          IL_0109:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::MoveNext()
          IL_010e:  brtrue     IL_008b

          IL_0113:  ldarg.0
          IL_0114:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>m__Finally1'()
          IL_0119:  ldarg.0
          IL_011a:  ldflda     valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>7__wrap3'
          IL_011f:  initobj    valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>
          IL_0125:  ldarg.0
          IL_0126:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<subNode>5__3'
          IL_012b:  stloc.3
          IL_012c:  ldarg.0
          IL_012d:  ldnull
          IL_012e:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<subNode>5__3'
          IL_0133:  ldloc.3
          IL_0134:  brfalse.s  IL_0157

          IL_0136:  ldarg.0
          IL_0137:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<i>5__2'
          IL_013c:  ldc.i4.1
          IL_013d:  add
          IL_013e:  stloc.s    V_6
          IL_0140:  ldarg.0
          IL_0141:  ldloc.s    V_6
          IL_0143:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<i>5__2'
          IL_0148:  ldloc.s    V_6
          IL_014a:  ldarg.0
          IL_014b:  ldfld      string[] System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::headersPath
          IL_0150:  ldlen
          IL_0151:  conv.i4
          IL_0152:  blt        IL_005e

          IL_0157:  ldc.i4.0
          IL_0158:  stloc.0
          IL_0159:  leave.s    IL_0162

        }  // end .try
        fault
        {
          IL_015b:  ldarg.0
          IL_015c:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::System.IDisposable.Dispose()
          IL_0161:  endfinally
        }  // end handler
        IL_0162:  ldloc.0
        IL_0163:  ret
      } // end of method '<FindSubnodes>d__11'::MoveNext

      .method private hidebysig instance void 
              '<>m__Finally1'() cil managed
      {
        // Code size       25 (0x19)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  ldc.i4.m1
        IL_0002:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>1__state'
        IL_0007:  ldarg.0
        IL_0008:  ldflda     valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>7__wrap3'
        IL_000d:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>
        IL_0013:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
        IL_0018:  ret
      } // end of method '<FindSubnodes>d__11'::'<>m__Finally1'

      .method private hidebysig newslot specialname virtual final 
              instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode 
              'System.Collections.Generic.IEnumerator<System.ServiceModel.Diagnostics.TraceXPathNavigator.ElementNode>.get_Current'() cil managed
      {
        .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
        .override  method instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode>::get_Current()
        // Code size       7 (0x7)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>2__current'
        IL_0006:  ret
      } // end of method '<FindSubnodes>d__11'::'System.Collections.Generic.IEnumerator<System.ServiceModel.Diagnostics.TraceXPathNavigator.ElementNode>.get_Current'

      .method private hidebysig newslot virtual final 
              instance void  System.Collections.IEnumerator.Reset() cil managed
      {
        .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
        .override [mscorlib]System.Collections.IEnumerator::Reset
        // Code size       6 (0x6)
        .maxstack  8
        IL_0000:  newobj     instance void [mscorlib]System.NotSupportedException::.ctor()
        IL_0005:  throw
      } // end of method '<FindSubnodes>d__11'::System.Collections.IEnumerator.Reset

      .method private hidebysig newslot specialname virtual final 
              instance object  System.Collections.IEnumerator.get_Current() cil managed
      {
        .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
        .override [mscorlib]System.Collections.IEnumerator::get_Current
        // Code size       7 (0x7)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>2__current'
        IL_0006:  ret
      } // end of method '<FindSubnodes>d__11'::System.Collections.IEnumerator.get_Current

      .method private hidebysig newslot virtual final 
              instance class [mscorlib]System.Collections.Generic.IEnumerator`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode> 
              'System.Collections.Generic.IEnumerable<System.ServiceModel.Diagnostics.TraceXPathNavigator.ElementNode>.GetEnumerator'() cil managed
      {
        .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
        .override  method instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode>::GetEnumerator()
        // Code size       67 (0x43)
        .maxstack  2
        .locals init (class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11' V_0)
        IL_0000:  ldarg.0
        IL_0001:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>1__state'
        IL_0006:  ldc.i4.s   -2
        IL_0008:  bne.un.s   IL_0022

        IL_000a:  ldarg.0
        IL_000b:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>l__initialThreadId'
        IL_0010:  call       int32 [mscorlib]System.Environment::get_CurrentManagedThreadId()
        IL_0015:  bne.un.s   IL_0022

        IL_0017:  ldarg.0
        IL_0018:  ldc.i4.0
        IL_0019:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>1__state'
        IL_001e:  ldarg.0
        IL_001f:  stloc.0
        IL_0020:  br.s       IL_0035

        IL_0022:  ldc.i4.0
        IL_0023:  newobj     instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::.ctor(int32)
        IL_0028:  stloc.0
        IL_0029:  ldloc.0
        IL_002a:  ldarg.0
        IL_002b:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>4__this'
        IL_0030:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>4__this'
        IL_0035:  ldloc.0
        IL_0036:  ldarg.0
        IL_0037:  ldfld      string[] System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>3__headersPath'
        IL_003c:  stfld      string[] System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::headersPath
        IL_0041:  ldloc.0
        IL_0042:  ret
      } // end of method '<FindSubnodes>d__11'::'System.Collections.Generic.IEnumerable<System.ServiceModel.Diagnostics.TraceXPathNavigator.ElementNode>.GetEnumerator'

      .method private hidebysig newslot virtual final 
              instance class [mscorlib]System.Collections.IEnumerator 
              System.Collections.IEnumerable.GetEnumerator() cil managed
      {
        .custom instance void [mscorlib]System.Diagnostics.DebuggerHiddenAttribute::.ctor() = ( 01 00 00 00 ) 
        .override [mscorlib]System.Collections.IEnumerable::GetEnumerator
        // Code size       7 (0x7)
        .maxstack  8
        IL_0000:  ldarg.0
        IL_0001:  call       instance class [mscorlib]System.Collections.Generic.IEnumerator`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'System.Collections.Generic.IEnumerable<System.ServiceModel.Diagnostics.TraceXPathNavigator.ElementNode>.GetEnumerator'()
        IL_0006:  ret
      } // end of method '<FindSubnodes>d__11'::System.Collections.IEnumerable.GetEnumerator

      .property instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode
              'System.Collections.Generic.IEnumerator<System.ServiceModel.Diagnostics.TraceXPathNavigator.ElementNode>.Current'()
      {
        .get instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'System.Collections.Generic.IEnumerator<System.ServiceModel.Diagnostics.TraceXPathNavigator.ElementNode>.get_Current'()
      } // end of property '<FindSubnodes>d__11'::'System.Collections.Generic.IEnumerator<System.ServiceModel.Diagnostics.TraceXPathNavigator.ElementNode>.Current'
      .property instance object System.Collections.IEnumerator.Current()
      {
        .get instance object System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::System.Collections.IEnumerator.get_Current()
      } // end of property '<FindSubnodes>d__11'::System.Collections.IEnumerator.Current
    } // end of class '<FindSubnodes>d__11'

    .field private int32 attributeIndex
    .field private int32 elementIndex
    .field assembly string name
    .field assembly string prefix
    .field assembly string xmlns
    .field assembly class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> childNodes
    .field assembly class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode> attributes
    .field assembly class System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode text
    .field assembly bool movedToText
    .method assembly hidebysig specialname rtspecialname 
            instance void  .ctor(string name,
                                 string prefix,
                                 class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode parent,
                                 string xmlns) cil managed
    {
      // Code size       53 (0x35)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::.ctor()
      IL_0006:  stfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
      IL_000b:  ldarg.0
      IL_000c:  newobj     instance void class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode>::.ctor()
      IL_0011:  stfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributes
      IL_0016:  ldarg.0
      IL_0017:  ldc.i4.1
      IL_0018:  ldarg.3
      IL_0019:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::.ctor(valuetype [System.Xml]System.Xml.XPath.XPathNodeType,
                                                                                                              class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode)
      IL_001e:  ldarg.0
      IL_001f:  ldarg.1
      IL_0020:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::name
      IL_0025:  ldarg.0
      IL_0026:  ldarg.2
      IL_0027:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::prefix
      IL_002c:  ldarg.0
      IL_002d:  ldarg.s    xmlns
      IL_002f:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::xmlns
      IL_0034:  ret
    } // end of method ElementNode::.ctor

    .method assembly hidebysig instance void 
            Add(class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode node) cil managed
    {
      // Code size       13 (0xd)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
      IL_0006:  ldarg.1
      IL_0007:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::Add(!0)
      IL_000c:  ret
    } // end of method ElementNode::Add

    .method assembly hidebysig instance class [mscorlib]System.Collections.Generic.IEnumerable`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode> 
            FindSubnodes(string[] headersPath) cil managed
    {
      .custom instance void [mscorlib]System.Runtime.CompilerServices.IteratorStateMachineAttribute::.ctor(class [mscorlib]System.Type) = ( 01 00 53 53 79 73 74 65 6D 2E 53 65 72 76 69 63   // ..SSystem.Servic
                                                                                                                                            65 4D 6F 64 65 6C 2E 44 69 61 67 6E 6F 73 74 69   // eModel.Diagnosti
                                                                                                                                            63 73 2E 54 72 61 63 65 58 50 61 74 68 4E 61 76   // cs.TraceXPathNav
                                                                                                                                            69 67 61 74 6F 72 2B 45 6C 65 6D 65 6E 74 4E 6F   // igator+ElementNo
                                                                                                                                            64 65 2B 3C 46 69 6E 64 53 75 62 6E 6F 64 65 73   // de+<FindSubnodes
                                                                                                                                            3E 64 5F 5F 31 31 00 00 )                         // >d__11..
      // Code size       22 (0x16)
      .maxstack  8
      IL_0000:  ldc.i4.s   -2
      IL_0002:  newobj     instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::.ctor(int32)
      IL_0007:  dup
      IL_0008:  ldarg.0
      IL_0009:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>4__this'
      IL_000e:  dup
      IL_000f:  ldarg.1
      IL_0010:  stfld      string[] System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode/'<FindSubnodes>d__11'::'<>3__headersPath'
      IL_0015:  ret
    } // end of method ElementNode::FindSubnodes

    .method assembly hidebysig instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode 
            MoveToNext() cil managed
    {
      // Code size       57 (0x39)
      .maxstack  3
      .locals init (class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode V_0)
      IL_0000:  ldnull
      IL_0001:  stloc.0
      IL_0002:  ldarg.0
      IL_0003:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::elementIndex
      IL_0008:  ldc.i4.1
      IL_0009:  add
      IL_000a:  ldarg.0
      IL_000b:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
      IL_0010:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::get_Count()
      IL_0015:  bge.s      IL_0037

      IL_0017:  ldarg.0
      IL_0018:  ldarg.0
      IL_0019:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::elementIndex
      IL_001e:  ldc.i4.1
      IL_001f:  add
      IL_0020:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::elementIndex
      IL_0025:  ldarg.0
      IL_0026:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
      IL_002b:  ldarg.0
      IL_002c:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::elementIndex
      IL_0031:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::get_Item(int32)
      IL_0036:  stloc.0
      IL_0037:  ldloc.0
      IL_0038:  ret
    } // end of method ElementNode::MoveToNext

    .method assembly hidebysig instance bool 
            MoveToFirstAttribute() cil managed
    {
      // Code size       32 (0x20)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.0
      IL_0002:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributeIndex
      IL_0007:  ldarg.0
      IL_0008:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributes
      IL_000d:  brfalse.s  IL_001e

      IL_000f:  ldarg.0
      IL_0010:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributes
      IL_0015:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode>::get_Count()
      IL_001a:  ldc.i4.0
      IL_001b:  cgt
      IL_001d:  ret

      IL_001e:  ldc.i4.0
      IL_001f:  ret
    } // end of method ElementNode::MoveToFirstAttribute

    .method assembly hidebysig instance bool 
            MoveToNextAttribute() cil managed
    {
      // Code size       41 (0x29)
      .maxstack  3
      .locals init (bool V_0)
      IL_0000:  ldc.i4.0
      IL_0001:  stloc.0
      IL_0002:  ldarg.0
      IL_0003:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributeIndex
      IL_0008:  ldc.i4.1
      IL_0009:  add
      IL_000a:  ldarg.0
      IL_000b:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributes
      IL_0010:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode>::get_Count()
      IL_0015:  bge.s      IL_0027

      IL_0017:  ldarg.0
      IL_0018:  ldarg.0
      IL_0019:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributeIndex
      IL_001e:  ldc.i4.1
      IL_001f:  add
      IL_0020:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributeIndex
      IL_0025:  ldc.i4.1
      IL_0026:  stloc.0
      IL_0027:  ldloc.0
      IL_0028:  ret
    } // end of method ElementNode::MoveToNextAttribute

    .method assembly hidebysig instance void 
            Reset() cil managed
    {
      // Code size       102 (0x66)
      .maxstack  2
      .locals init (valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> V_0,
               class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode V_1,
               class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode V_2)
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.0
      IL_0002:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributeIndex
      IL_0007:  ldarg.0
      IL_0008:  ldc.i4.0
      IL_0009:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::elementIndex
      IL_000e:  ldarg.0
      IL_000f:  ldc.i4.0
      IL_0010:  stfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::movedToText
      IL_0015:  ldarg.0
      IL_0016:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
      IL_001b:  brfalse.s  IL_0065

      IL_001d:  ldarg.0
      IL_001e:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
      IL_0023:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::GetEnumerator()
      IL_0028:  stloc.0
      .try
      {
        IL_0029:  br.s       IL_004c

        IL_002b:  ldloca.s   V_0
        IL_002d:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::get_Current()
        IL_0032:  stloc.1
        IL_0033:  ldloc.1
        IL_0034:  callvirt   instance valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::get_NodeType()
        IL_0039:  ldc.i4.1
        IL_003a:  bne.un.s   IL_004c

        IL_003c:  ldloc.1
        IL_003d:  isinst     System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode
        IL_0042:  stloc.2
        IL_0043:  ldloc.2
        IL_0044:  brfalse.s  IL_004c

        IL_0046:  ldloc.2
        IL_0047:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::Reset()
        IL_004c:  ldloca.s   V_0
        IL_004e:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::MoveNext()
        IL_0053:  brtrue.s   IL_002b

        IL_0055:  leave.s    IL_0065

      }  // end .try
      finally
      {
        IL_0057:  ldloca.s   V_0
        IL_0059:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>
        IL_005f:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
        IL_0064:  endfinally
      }  // end handler
      IL_0065:  ret
    } // end of method ElementNode::Reset

    .method assembly hidebysig specialname 
            instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode 
            get_CurrentAttribute() cil managed
    {
      // Code size       18 (0x12)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributes
      IL_0006:  ldarg.0
      IL_0007:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributeIndex
      IL_000c:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode>::get_Item(int32)
      IL_0011:  ret
    } // end of method ElementNode::get_CurrentAttribute

    .method public hidebysig newslot specialname virtual final 
            instance int32  get_Size() cil managed
    {
      // Code size       77 (0x4d)
      .maxstack  3
      .locals init (int32 V_0)
      IL_0000:  ldc.i4.2
      IL_0001:  ldarg.0
      IL_0002:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::name
      IL_0007:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_000c:  mul
      IL_000d:  ldc.i4.6
      IL_000e:  add
      IL_000f:  stloc.0
      IL_0010:  ldarg.0
      IL_0011:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::prefix
      IL_0016:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
      IL_001b:  brtrue.s   IL_002d

      IL_001d:  ldloc.0
      IL_001e:  ldarg.0
      IL_001f:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::prefix
      IL_0024:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_0029:  ldc.i4.1
      IL_002a:  add
      IL_002b:  add
      IL_002c:  stloc.0
      IL_002d:  ldarg.0
      IL_002e:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::xmlns
      IL_0033:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
      IL_0038:  brtrue.s   IL_004b

      IL_003a:  ldloc.0
      IL_003b:  ldarg.0
      IL_003c:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::xmlns
      IL_0041:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_0046:  ldc.i4.s   9
      IL_0048:  add
      IL_0049:  add
      IL_004a:  stloc.0
      IL_004b:  ldloc.0
      IL_004c:  ret
    } // end of method ElementNode::get_Size

    .property instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode
            CurrentAttribute()
    {
      .get instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::get_CurrentAttribute()
    } // end of property ElementNode::CurrentAttribute
    .property instance int32 Size()
    {
      .get instance int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::get_Size()
    } // end of property ElementNode::Size
  } // end of class ElementNode

  .class auto ansi nested private beforefieldinit AttributeNode
         extends [mscorlib]System.Object
         implements System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable
  {
    .field assembly string name
    .field assembly string nodeValue
    .field assembly string prefix
    .field assembly string xmlns
    .method assembly hidebysig specialname rtspecialname 
            instance void  .ctor(string name,
                                 string prefix,
                                 string 'value',
                                 string xmlns) cil managed
    {
      // Code size       36 (0x24)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::name
      IL_000d:  ldarg.0
      IL_000e:  ldarg.2
      IL_000f:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::prefix
      IL_0014:  ldarg.0
      IL_0015:  ldarg.3
      IL_0016:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::nodeValue
      IL_001b:  ldarg.0
      IL_001c:  ldarg.s    xmlns
      IL_001e:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::xmlns
      IL_0023:  ret
    } // end of method AttributeNode::.ctor

    .method public hidebysig newslot specialname virtual final 
            instance int32  get_Size() cil managed
    {
      // Code size       87 (0x57)
      .maxstack  3
      .locals init (int32 V_0)
      IL_0000:  ldarg.0
      IL_0001:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::name
      IL_0006:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_000b:  ldarg.0
      IL_000c:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::nodeValue
      IL_0011:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_0016:  add
      IL_0017:  ldc.i4.5
      IL_0018:  add
      IL_0019:  stloc.0
      IL_001a:  ldarg.0
      IL_001b:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::prefix
      IL_0020:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
      IL_0025:  brtrue.s   IL_0037

      IL_0027:  ldloc.0
      IL_0028:  ldarg.0
      IL_0029:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::prefix
      IL_002e:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_0033:  ldc.i4.1
      IL_0034:  add
      IL_0035:  add
      IL_0036:  stloc.0
      IL_0037:  ldarg.0
      IL_0038:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::xmlns
      IL_003d:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
      IL_0042:  brtrue.s   IL_0055

      IL_0044:  ldloc.0
      IL_0045:  ldarg.0
      IL_0046:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::xmlns
      IL_004b:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_0050:  ldc.i4.s   9
      IL_0052:  add
      IL_0053:  add
      IL_0054:  stloc.0
      IL_0055:  ldloc.0
      IL_0056:  ret
    } // end of method AttributeNode::get_Size

    .property instance int32 Size()
    {
      .get instance int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::get_Size()
    } // end of property AttributeNode::Size
  } // end of class AttributeNode

  .class auto ansi nested private beforefieldinit ProcessingInstructionNode
         extends System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode
         implements System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable
  {
    .field assembly string name
    .field assembly string text
    .method assembly hidebysig specialname rtspecialname 
            instance void  .ctor(string name,
                                 string text,
                                 class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode parent) cil managed
    {
      // Code size       23 (0x17)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldc.i4.7
      IL_0002:  ldarg.3
      IL_0003:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::.ctor(valuetype [System.Xml]System.Xml.XPath.XPathNodeType,
                                                                                                              class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode)
      IL_0008:  ldarg.0
      IL_0009:  ldarg.1
      IL_000a:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode::name
      IL_000f:  ldarg.0
      IL_0010:  ldarg.2
      IL_0011:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode::text
      IL_0016:  ret
    } // end of method ProcessingInstructionNode::.ctor

    .method public hidebysig newslot specialname virtual final 
            instance int32  get_Size() cil managed
    {
      // Code size       27 (0x1b)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode::name
      IL_0006:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_000b:  ldarg.0
      IL_000c:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode::text
      IL_0011:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_0016:  add
      IL_0017:  ldc.i4.s   12
      IL_0019:  add
      IL_001a:  ret
    } // end of method ProcessingInstructionNode::get_Size

    .property instance int32 Size()
    {
      .get instance int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode::get_Size()
    } // end of property ProcessingInstructionNode::Size
  } // end of class ProcessingInstructionNode

  .class auto ansi nested private beforefieldinit TextNode
         extends [mscorlib]System.Object
         implements System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable
  {
    .field assembly string nodeValue
    .method assembly hidebysig specialname rtspecialname 
            instance void  .ctor(string 'value') cil managed
    {
      // Code size       14 (0xe)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
      IL_0006:  ldarg.0
      IL_0007:  ldarg.1
      IL_0008:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode::nodeValue
      IL_000d:  ret
    } // end of method TextNode::.ctor

    .method public hidebysig newslot specialname virtual final 
            instance int32  get_Size() cil managed
    {
      // Code size       12 (0xc)
      .maxstack  8
      IL_0000:  ldarg.0
      IL_0001:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode::nodeValue
      IL_0006:  callvirt   instance int32 [mscorlib]System.String::get_Length()
      IL_000b:  ret
    } // end of method TextNode::get_Size

    .property instance int32 Size()
    {
      .get instance int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode::get_Size()
    } // end of property TextNode::Size
  } // end of class TextNode

  .field private static literal int32 UnlimitedSize = int32(0xFFFFFFFF)
  .field private class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode root
  .field private class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode current
  .field private bool closed
  .field private valuetype [System.Xml]System.Xml.XPath.XPathNodeType state
  .field private int32 maxSize
  .field private int64 currentSize
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(int32 maxSize) cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldc.i4.1
    IL_0002:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0007:  ldarg.0
    IL_0008:  call       instance void [System.Xml]System.Xml.XPath.XPathNavigator::.ctor()
    IL_000d:  ldarg.0
    IL_000e:  ldarg.1
    IL_000f:  stfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator::maxSize
    IL_0014:  ldarg.0
    IL_0015:  ldc.i4.0
    IL_0016:  conv.i8
    IL_0017:  stfld      int64 System.ServiceModel.Diagnostics.TraceXPathNavigator::currentSize
    IL_001c:  ret
  } // end of method TraceXPathNavigator::.ctor

  .method assembly hidebysig instance void 
          AddElement(string prefix,
                     string name,
                     string xmlns) cil managed
  {
    // Code size       99 (0x63)
    .maxstack  4
    .locals init (class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator::closed
    IL_0006:  brfalse.s  IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldarg.2
    IL_000f:  ldarg.1
    IL_0010:  ldarg.0
    IL_0011:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0016:  ldarg.3
    IL_0017:  newobj     instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::.ctor(string,
                                                                                                              string,
                                                                                                              class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode,
                                                                                                              string)
    IL_001c:  stloc.0
    IL_001d:  ldarg.0
    IL_001e:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0023:  brtrue.s   IL_0040

    IL_0025:  ldarg.0
    IL_0026:  ldloc.0
    IL_0027:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::VerifySize(class System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable)
    IL_002c:  ldarg.0
    IL_002d:  ldloc.0
    IL_002e:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::root
    IL_0033:  ldarg.0
    IL_0034:  ldarg.0
    IL_0035:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::root
    IL_003a:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_003f:  ret

    IL_0040:  ldarg.0
    IL_0041:  ldfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator::closed
    IL_0046:  brtrue.s   IL_0062

    IL_0048:  ldarg.0
    IL_0049:  ldloc.0
    IL_004a:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::VerifySize(class System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable)
    IL_004f:  ldarg.0
    IL_0050:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0055:  ldloc.0
    IL_0056:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::Add(class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode)
    IL_005b:  ldarg.0
    IL_005c:  ldloc.0
    IL_005d:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0062:  ret
  } // end of method TraceXPathNavigator::AddElement

  .method assembly hidebysig instance void 
          AddProcessingInstruction(string name,
                                   string text) cil managed
  {
    // Code size       43 (0x2b)
    .maxstack  3
    .locals init (class System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0006:  brtrue.s   IL_0009

    IL_0008:  ret

    IL_0009:  ldarg.1
    IL_000a:  ldarg.2
    IL_000b:  ldarg.0
    IL_000c:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0011:  newobj     instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode::.ctor(string,
                                                                                                                            string,
                                                                                                                            class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode)
    IL_0016:  stloc.0
    IL_0017:  ldarg.0
    IL_0018:  ldloc.0
    IL_0019:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::VerifySize(class System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable)
    IL_001e:  ldarg.0
    IL_001f:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0024:  ldloc.0
    IL_0025:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::Add(class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode)
    IL_002a:  ret
  } // end of method TraceXPathNavigator::AddProcessingInstruction

  .method assembly hidebysig instance void 
          AddText(string 'value') cil managed
  {
    // Code size       107 (0x6b)
    .maxstack  3
    .locals init (class System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator::closed
    IL_0006:  brfalse.s  IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldarg.0
    IL_000f:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0014:  brtrue.s   IL_0017

    IL_0016:  ret

    IL_0017:  ldarg.0
    IL_0018:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_001d:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::text
    IL_0022:  brtrue.s   IL_003f

    IL_0024:  ldarg.1
    IL_0025:  newobj     instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode::.ctor(string)
    IL_002a:  stloc.0
    IL_002b:  ldarg.0
    IL_002c:  ldloc.0
    IL_002d:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::VerifySize(class System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable)
    IL_0032:  ldarg.0
    IL_0033:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0038:  ldloc.0
    IL_0039:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::text
    IL_003e:  ret

    IL_003f:  ldarg.1
    IL_0040:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
    IL_0045:  brtrue.s   IL_006a

    IL_0047:  ldarg.0
    IL_0048:  ldarg.1
    IL_0049:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::VerifySize(string)
    IL_004e:  ldarg.0
    IL_004f:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0054:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::text
    IL_0059:  dup
    IL_005a:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode::nodeValue
    IL_005f:  ldarg.1
    IL_0060:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0065:  stfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode::nodeValue
    IL_006a:  ret
  } // end of method TraceXPathNavigator::AddText

  .method assembly hidebysig instance void 
          AddAttribute(string name,
                       string 'value',
                       string xmlns,
                       string prefix) cil managed
  {
    // Code size       64 (0x40)
    .maxstack  4
    .locals init (class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator::closed
    IL_0006:  brfalse.s  IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldarg.0
    IL_000f:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0014:  brtrue.s   IL_001c

    IL_0016:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_001b:  throw

    IL_001c:  ldarg.1
    IL_001d:  ldarg.s    prefix
    IL_001f:  ldarg.2
    IL_0020:  ldarg.3
    IL_0021:  newobj     instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::.ctor(string,
                                                                                                                string,
                                                                                                                string,
                                                                                                                string)
    IL_0026:  stloc.0
    IL_0027:  ldarg.0
    IL_0028:  ldloc.0
    IL_0029:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::VerifySize(class System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable)
    IL_002e:  ldarg.0
    IL_002f:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0034:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributes
    IL_0039:  ldloc.0
    IL_003a:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode>::Add(!0)
    IL_003f:  ret
  } // end of method TraceXPathNavigator::AddAttribute

  .method assembly hidebysig instance void 
          AddComment(string text) cil managed
  {
    // Code size       61 (0x3d)
    .maxstack  2
    .locals init (class System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator::closed
    IL_0006:  brfalse.s  IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldarg.0
    IL_000f:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0014:  brtrue.s   IL_001c

    IL_0016:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_001b:  throw

    IL_001c:  ldarg.1
    IL_001d:  ldarg.0
    IL_001e:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0023:  newobj     instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode::.ctor(string,
                                                                                                              class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode)
    IL_0028:  stloc.0
    IL_0029:  ldarg.0
    IL_002a:  ldloc.0
    IL_002b:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::VerifySize(class System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable)
    IL_0030:  ldarg.0
    IL_0031:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0036:  ldloc.0
    IL_0037:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::Add(class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode)
    IL_003c:  ret
  } // end of method TraceXPathNavigator::AddComment

  .method assembly hidebysig instance void 
          CloseElement() cil managed
  {
    // Code size       47 (0x2f)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator::closed
    IL_0006:  brfalse.s  IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldarg.0
    IL_000f:  ldarg.0
    IL_0010:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0015:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::parent
    IL_001a:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_001f:  ldarg.0
    IL_0020:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0025:  brtrue.s   IL_002e

    IL_0027:  ldarg.0
    IL_0028:  ldc.i4.1
    IL_0029:  stfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator::closed
    IL_002e:  ret
  } // end of method TraceXPathNavigator::CloseElement

  .method public hidebysig specialname virtual 
          instance string  get_BaseURI() cil managed
  {
    // Code size       6 (0x6)
    .maxstack  8
    IL_0000:  ldsfld     string [mscorlib]System.String::Empty
    IL_0005:  ret
  } // end of method TraceXPathNavigator::get_BaseURI

  .method public hidebysig virtual instance class [System.Xml]System.Xml.XPath.XPathNavigator 
          Clone() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ret
  } // end of method TraceXPathNavigator::Clone

  .method public hidebysig specialname virtual 
          instance bool  get_IsEmptyElement() cil managed
  {
    // Code size       48 (0x30)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  ldc.i4.1
    IL_0001:  stloc.0
    IL_0002:  ldarg.0
    IL_0003:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0008:  brfalse.s  IL_002e

    IL_000a:  ldarg.0
    IL_000b:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0010:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::text
    IL_0015:  brtrue.s   IL_002c

    IL_0017:  ldarg.0
    IL_0018:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_001d:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
    IL_0022:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::get_Count()
    IL_0027:  ldc.i4.0
    IL_0028:  cgt
    IL_002a:  br.s       IL_002d

    IL_002c:  ldc.i4.1
    IL_002d:  stloc.0
    IL_002e:  ldloc.0
    IL_002f:  ret
  } // end of method TraceXPathNavigator::get_IsEmptyElement

  .method public hidebysig virtual instance bool 
          IsSamePosition(class [System.Xml]System.Xml.XPath.XPathNavigator other) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  } // end of method TraceXPathNavigator::IsSamePosition

  .method public hidebysig specialname virtual 
          instance string  get_LocalName() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  callvirt   instance string [System.Xml]System.Xml.XPath.XPathNavigator::get_Name()
    IL_0006:  ret
  } // end of method TraceXPathNavigator::get_LocalName

  .method public hidebysig virtual instance string 
          LookupPrefix(string ns) cil managed
  {
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldarg.0
    IL_0003:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0008:  call       instance string System.ServiceModel.Diagnostics.TraceXPathNavigator::LookupPrefix(string,
                                                                                                           class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode)
    IL_000d:  ret
  } // end of method TraceXPathNavigator::LookupPrefix

  .method private hidebysig instance string 
          LookupPrefix(string ns,
                       class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode node) cil managed
  {
    // Code size       143 (0x8f)
    .maxstack  3
    .locals init (string V_0,
             valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode> V_1,
             class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode V_2)
    IL_0000:  ldnull
    IL_0001:  stloc.0
    IL_0002:  ldarg.1
    IL_0003:  ldarg.2
    IL_0004:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::xmlns
    IL_0009:  ldc.i4.4
    IL_000a:  call       int32 [mscorlib]System.String::Compare(string,
                                                                string,
                                                                valuetype [mscorlib]System.StringComparison)
    IL_000f:  brtrue.s   IL_001a

    IL_0011:  ldarg.2
    IL_0012:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::prefix
    IL_0017:  stloc.0
    IL_0018:  br.s       IL_0074

    IL_001a:  ldarg.2
    IL_001b:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributes
    IL_0020:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode>::GetEnumerator()
    IL_0025:  stloc.1
    .try
    {
      IL_0026:  br.s       IL_005b

      IL_0028:  ldloca.s   V_1
      IL_002a:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode>::get_Current()
      IL_002f:  stloc.2
      IL_0030:  ldstr      "xmlns"
      IL_0035:  ldloc.2
      IL_0036:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::prefix
      IL_003b:  ldc.i4.4
      IL_003c:  call       int32 [mscorlib]System.String::Compare(string,
                                                                  string,
                                                                  valuetype [mscorlib]System.StringComparison)
      IL_0041:  brtrue.s   IL_005b

      IL_0043:  ldarg.1
      IL_0044:  ldloc.2
      IL_0045:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::nodeValue
      IL_004a:  ldc.i4.4
      IL_004b:  call       int32 [mscorlib]System.String::Compare(string,
                                                                  string,
                                                                  valuetype [mscorlib]System.StringComparison)
      IL_0050:  brtrue.s   IL_005b

      IL_0052:  ldloc.2
      IL_0053:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::name
      IL_0058:  stloc.0
      IL_0059:  leave.s    IL_0074

      IL_005b:  ldloca.s   V_1
      IL_005d:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode>::MoveNext()
      IL_0062:  brtrue.s   IL_0028

      IL_0064:  leave.s    IL_0074

    }  // end .try
    finally
    {
      IL_0066:  ldloca.s   V_1
      IL_0068:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode>
      IL_006e:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_0073:  endfinally
    }  // end handler
    IL_0074:  ldloc.0
    IL_0075:  brtrue.s   IL_008d

    IL_0077:  ldarg.2
    IL_0078:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::parent
    IL_007d:  brfalse.s  IL_008d

    IL_007f:  ldarg.0
    IL_0080:  ldarg.1
    IL_0081:  ldarg.2
    IL_0082:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::parent
    IL_0087:  call       instance string System.ServiceModel.Diagnostics.TraceXPathNavigator::LookupPrefix(string,
                                                                                                           class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode)
    IL_008c:  stloc.0
    IL_008d:  ldloc.0
    IL_008e:  ret
  } // end of method TraceXPathNavigator::LookupPrefix

  .method public hidebysig virtual instance bool 
          MoveTo(class [System.Xml]System.Xml.XPath.XPathNavigator other) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  } // end of method TraceXPathNavigator::MoveTo

  .method public hidebysig virtual instance bool 
          MoveToFirstAttribute() cil managed
  {
    // Code size       38 (0x26)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0006:  brtrue.s   IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldarg.0
    IL_000f:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0014:  callvirt   instance bool System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::MoveToFirstAttribute()
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  brfalse.s  IL_0024

    IL_001d:  ldarg.0
    IL_001e:  ldc.i4.2
    IL_001f:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0024:  ldloc.0
    IL_0025:  ret
  } // end of method TraceXPathNavigator::MoveToFirstAttribute

  .method public hidebysig virtual instance bool 
          MoveToFirstChild() cil managed
  {
    // Code size       159 (0x9f)
    .maxstack  3
    .locals init (bool V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0006:  brtrue.s   IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldc.i4.0
    IL_000f:  stloc.0
    IL_0010:  ldarg.0
    IL_0011:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0016:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
    IL_001b:  brfalse.s  IL_005c

    IL_001d:  ldarg.0
    IL_001e:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0023:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
    IL_0028:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::get_Count()
    IL_002d:  ldc.i4.0
    IL_002e:  ble.s      IL_005c

    IL_0030:  ldarg.0
    IL_0031:  ldarg.0
    IL_0032:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0037:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
    IL_003c:  ldc.i4.0
    IL_003d:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::get_Item(int32)
    IL_0042:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0047:  ldarg.0
    IL_0048:  ldarg.0
    IL_0049:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_004e:  callvirt   instance valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::get_NodeType()
    IL_0053:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0058:  ldc.i4.1
    IL_0059:  stloc.0
    IL_005a:  br.s       IL_009d

    IL_005c:  ldarg.0
    IL_005d:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0062:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
    IL_0067:  brfalse.s  IL_007b

    IL_0069:  ldarg.0
    IL_006a:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_006f:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
    IL_0074:  callvirt   instance int32 class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::get_Count()
    IL_0079:  brtrue.s   IL_009d

    IL_007b:  ldarg.0
    IL_007c:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0081:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::text
    IL_0086:  brfalse.s  IL_009d

    IL_0088:  ldarg.0
    IL_0089:  ldc.i4.4
    IL_008a:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_008f:  ldarg.0
    IL_0090:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0095:  ldc.i4.1
    IL_0096:  stfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::movedToText
    IL_009b:  ldc.i4.1
    IL_009c:  stloc.0
    IL_009d:  ldloc.0
    IL_009e:  ret
  } // end of method TraceXPathNavigator::MoveToFirstChild

  .method public hidebysig virtual instance bool 
          MoveToFirstNamespace(valuetype [System.Xml]System.Xml.XPath.XPathNamespaceScope namespaceScope) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  } // end of method TraceXPathNavigator::MoveToFirstNamespace

  .method public hidebysig virtual instance bool 
          MoveToId(string id) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  } // end of method TraceXPathNavigator::MoveToId

  .method public hidebysig virtual instance bool 
          MoveToNext() cil managed
  {
    // Code size       117 (0x75)
    .maxstack  2
    .locals init (bool V_0,
             class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode V_1,
             class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode V_2)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0006:  brtrue.s   IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldc.i4.0
    IL_000f:  stloc.0
    IL_0010:  ldarg.0
    IL_0011:  ldfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0016:  ldc.i4.4
    IL_0017:  beq.s      IL_0073

    IL_0019:  ldarg.0
    IL_001a:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_001f:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::parent
    IL_0024:  stloc.1
    IL_0025:  ldloc.1
    IL_0026:  brfalse.s  IL_0073

    IL_0028:  ldloc.1
    IL_0029:  callvirt   instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::MoveToNext()
    IL_002e:  stloc.2
    IL_002f:  ldloc.2
    IL_0030:  brtrue.s   IL_005b

    IL_0032:  ldloc.1
    IL_0033:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::text
    IL_0038:  brfalse.s  IL_005b

    IL_003a:  ldloc.1
    IL_003b:  ldfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::movedToText
    IL_0040:  brtrue.s   IL_005b

    IL_0042:  ldarg.0
    IL_0043:  ldc.i4.4
    IL_0044:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0049:  ldloc.1
    IL_004a:  ldc.i4.1
    IL_004b:  stfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::movedToText
    IL_0050:  ldarg.0
    IL_0051:  ldloc.1
    IL_0052:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0057:  ldc.i4.1
    IL_0058:  stloc.0
    IL_0059:  br.s       IL_0073

    IL_005b:  ldloc.2
    IL_005c:  brfalse.s  IL_0073

    IL_005e:  ldarg.0
    IL_005f:  ldloc.2
    IL_0060:  callvirt   instance valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::get_NodeType()
    IL_0065:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_006a:  ldc.i4.1
    IL_006b:  stloc.0
    IL_006c:  ldarg.0
    IL_006d:  ldloc.2
    IL_006e:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0073:  ldloc.0
    IL_0074:  ret
  } // end of method TraceXPathNavigator::MoveToNext

  .method public hidebysig virtual instance bool 
          MoveToNextAttribute() cil managed
  {
    // Code size       38 (0x26)
    .maxstack  2
    .locals init (bool V_0)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0006:  brtrue.s   IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldarg.0
    IL_000f:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0014:  callvirt   instance bool System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::MoveToNextAttribute()
    IL_0019:  stloc.0
    IL_001a:  ldloc.0
    IL_001b:  brfalse.s  IL_0024

    IL_001d:  ldarg.0
    IL_001e:  ldc.i4.2
    IL_001f:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0024:  ldloc.0
    IL_0025:  ret
  } // end of method TraceXPathNavigator::MoveToNextAttribute

  .method public hidebysig virtual instance bool 
          MoveToNextNamespace(valuetype [System.Xml]System.Xml.XPath.XPathNamespaceScope namespaceScope) cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  } // end of method TraceXPathNavigator::MoveToNextNamespace

  .method public hidebysig virtual instance bool 
          MoveToParent() cil managed
  {
    // Code size       149 (0x95)
    .maxstack  2
    .locals init (bool V_0,
             valuetype [System.Xml]System.Xml.XPath.XPathNodeType V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0006:  brtrue.s   IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldc.i4.0
    IL_000f:  stloc.0
    IL_0010:  ldarg.0
    IL_0011:  ldfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0016:  stloc.1
    IL_0017:  ldloc.1
    IL_0018:  ldc.i4.1
    IL_0019:  sub
    IL_001a:  switch     ( 
                          IL_0041,
                          IL_0074,
                          IL_008a,
                          IL_007f,
                          IL_0093,
                          IL_0093,
                          IL_0041,
                          IL_0041)
    IL_003f:  br.s       IL_0093

    IL_0041:  ldarg.0
    IL_0042:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0047:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::parent
    IL_004c:  brfalse.s  IL_0093

    IL_004e:  ldarg.0
    IL_004f:  ldarg.0
    IL_0050:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0055:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::parent
    IL_005a:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_005f:  ldarg.0
    IL_0060:  ldarg.0
    IL_0061:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0066:  callvirt   instance valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode::get_NodeType()
    IL_006b:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0070:  ldc.i4.1
    IL_0071:  stloc.0
    IL_0072:  br.s       IL_0093

    IL_0074:  ldarg.0
    IL_0075:  ldc.i4.1
    IL_0076:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_007b:  ldc.i4.1
    IL_007c:  stloc.0
    IL_007d:  br.s       IL_0093

    IL_007f:  ldarg.0
    IL_0080:  ldc.i4.1
    IL_0081:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0086:  ldc.i4.1
    IL_0087:  stloc.0
    IL_0088:  br.s       IL_0093

    IL_008a:  ldarg.0
    IL_008b:  ldc.i4.1
    IL_008c:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0091:  ldc.i4.1
    IL_0092:  stloc.0
    IL_0093:  ldloc.0
    IL_0094:  ret
  } // end of method TraceXPathNavigator::MoveToParent

  .method public hidebysig virtual instance bool 
          MoveToPrevious() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldc.i4.0
    IL_0001:  ret
  } // end of method TraceXPathNavigator::MoveToPrevious

  .method public hidebysig virtual instance void 
          MoveToRoot() cil managed
  {
    // Code size       31 (0x1f)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.0
    IL_0002:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::root
    IL_0007:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_000c:  ldarg.0
    IL_000d:  ldc.i4.1
    IL_000e:  stfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0013:  ldarg.0
    IL_0014:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::root
    IL_0019:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::Reset()
    IL_001e:  ret
  } // end of method TraceXPathNavigator::MoveToRoot

  .method public hidebysig specialname virtual 
          instance string  get_Name() cil managed
  {
    // Code size       82 (0x52)
    .maxstack  2
    .locals init (string V_0,
             valuetype [System.Xml]System.Xml.XPath.XPathNodeType V_1)
    IL_0000:  ldsfld     string [mscorlib]System.String::Empty
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_000c:  brfalse.s  IL_0050

    IL_000e:  ldarg.0
    IL_000f:  ldfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0014:  stloc.1
    IL_0015:  ldloc.1
    IL_0016:  ldc.i4.1
    IL_0017:  beq.s      IL_0036

    IL_0019:  ldloc.1
    IL_001a:  ldc.i4.2
    IL_001b:  beq.s      IL_0023

    IL_001d:  ldloc.1
    IL_001e:  ldc.i4.7
    IL_001f:  beq.s      IL_0044

    IL_0021:  br.s       IL_0050

    IL_0023:  ldarg.0
    IL_0024:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0029:  callvirt   instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::get_CurrentAttribute()
    IL_002e:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::name
    IL_0033:  stloc.0
    IL_0034:  br.s       IL_0050

    IL_0036:  ldarg.0
    IL_0037:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_003c:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::name
    IL_0041:  stloc.0
    IL_0042:  br.s       IL_0050

    IL_0044:  ldarg.0
    IL_0045:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentProcessingInstruction()
    IL_004a:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode::name
    IL_004f:  stloc.0
    IL_0050:  ldloc.0
    IL_0051:  ret
  } // end of method TraceXPathNavigator::get_Name

  .method public hidebysig specialname virtual 
          instance class [System.Xml]System.Xml.XmlNameTable 
          get_NameTable() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldnull
    IL_0001:  ret
  } // end of method TraceXPathNavigator::get_NameTable

  .method public hidebysig specialname virtual 
          instance string  get_NamespaceURI() cil managed
  {
    // Code size       80 (0x50)
    .maxstack  2
    .locals init (string V_0,
             valuetype [System.Xml]System.Xml.XPath.XPathNodeType V_1)
    IL_0000:  ldsfld     string [mscorlib]System.String::Empty
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_000c:  brfalse.s  IL_004e

    IL_000e:  ldarg.0
    IL_000f:  ldfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0014:  stloc.1
    IL_0015:  ldloc.1
    IL_0016:  ldc.i4.1
    IL_0017:  sub
    IL_0018:  switch     ( 
                          IL_002b,
                          IL_0039,
                          IL_004c)
    IL_0029:  br.s       IL_004e

    IL_002b:  ldarg.0
    IL_002c:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0031:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::xmlns
    IL_0036:  stloc.0
    IL_0037:  br.s       IL_004e

    IL_0039:  ldarg.0
    IL_003a:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_003f:  callvirt   instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::get_CurrentAttribute()
    IL_0044:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::xmlns
    IL_0049:  stloc.0
    IL_004a:  br.s       IL_004e

    IL_004c:  ldnull
    IL_004d:  stloc.0
    IL_004e:  ldloc.0
    IL_004f:  ret
  } // end of method TraceXPathNavigator::get_NamespaceURI

  .method public hidebysig specialname virtual 
          instance valuetype [System.Xml]System.Xml.XPath.XPathNodeType 
          get_NodeType() cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0006:  ret
  } // end of method TraceXPathNavigator::get_NodeType

  .method public hidebysig specialname virtual 
          instance string  get_Prefix() cil managed
  {
    // Code size       80 (0x50)
    .maxstack  2
    .locals init (string V_0,
             valuetype [System.Xml]System.Xml.XPath.XPathNodeType V_1)
    IL_0000:  ldsfld     string [mscorlib]System.String::Empty
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_000c:  brfalse.s  IL_004e

    IL_000e:  ldarg.0
    IL_000f:  ldfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0014:  stloc.1
    IL_0015:  ldloc.1
    IL_0016:  ldc.i4.1
    IL_0017:  sub
    IL_0018:  switch     ( 
                          IL_002b,
                          IL_0039,
                          IL_004c)
    IL_0029:  br.s       IL_004e

    IL_002b:  ldarg.0
    IL_002c:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0031:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::prefix
    IL_0036:  stloc.0
    IL_0037:  br.s       IL_004e

    IL_0039:  ldarg.0
    IL_003a:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_003f:  callvirt   instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::get_CurrentAttribute()
    IL_0044:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::prefix
    IL_0049:  stloc.0
    IL_004a:  br.s       IL_004e

    IL_004c:  ldnull
    IL_004d:  stloc.0
    IL_004e:  ldloc.0
    IL_004f:  ret
  } // end of method TraceXPathNavigator::get_Prefix

  .method private hidebysig specialname instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode 
          get_CurrentComment() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0006:  isinst     System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode
    IL_000b:  ret
  } // end of method TraceXPathNavigator::get_CurrentComment

  .method private hidebysig specialname instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode 
          get_CurrentElement() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0006:  isinst     System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode
    IL_000b:  ret
  } // end of method TraceXPathNavigator::get_CurrentElement

  .method private hidebysig specialname instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode 
          get_CurrentProcessingInstruction() cil managed
  {
    // Code size       12 (0xc)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0006:  isinst     System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode
    IL_000b:  ret
  } // end of method TraceXPathNavigator::get_CurrentProcessingInstruction

  .method public hidebysig specialname virtual 
          instance string  get_Value() cil managed
  {
    // Code size       125 (0x7d)
    .maxstack  2
    .locals init (string V_0,
             valuetype [System.Xml]System.Xml.XPath.XPathNodeType V_1)
    IL_0000:  ldsfld     string [mscorlib]System.String::Empty
    IL_0005:  stloc.0
    IL_0006:  ldarg.0
    IL_0007:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_000c:  brfalse.s  IL_007b

    IL_000e:  ldarg.0
    IL_000f:  ldfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0014:  stloc.1
    IL_0015:  ldloc.1
    IL_0016:  ldc.i4.2
    IL_0017:  sub
    IL_0018:  switch     ( 
                          IL_004e,
                          IL_007b,
                          IL_003b,
                          IL_007b,
                          IL_007b,
                          IL_006f,
                          IL_0061)
    IL_0039:  br.s       IL_007b

    IL_003b:  ldarg.0
    IL_003c:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0041:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::text
    IL_0046:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode::nodeValue
    IL_004b:  stloc.0
    IL_004c:  br.s       IL_007b

    IL_004e:  ldarg.0
    IL_004f:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
    IL_0054:  callvirt   instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::get_CurrentAttribute()
    IL_0059:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode::nodeValue
    IL_005e:  stloc.0
    IL_005f:  br.s       IL_007b

    IL_0061:  ldarg.0
    IL_0062:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentComment()
    IL_0067:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode::nodeValue
    IL_006c:  stloc.0
    IL_006d:  br.s       IL_007b

    IL_006f:  ldarg.0
    IL_0070:  call       instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentProcessingInstruction()
    IL_0075:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode::text
    IL_007a:  stloc.0
    IL_007b:  ldloc.0
    IL_007c:  ret
  } // end of method TraceXPathNavigator::get_Value

  .method assembly hidebysig specialname 
          instance valuetype [System.Xml]System.Xml.WriteState 
          get_WriteState() cil managed
  {
    // Code size       79 (0x4f)
    .maxstack  2
    .locals init (valuetype [System.Xml]System.Xml.WriteState V_0,
             valuetype [System.Xml]System.Xml.XPath.XPathNodeType V_1)
    IL_0000:  ldc.i4.6
    IL_0001:  stloc.0
    IL_0002:  ldarg.0
    IL_0003:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode System.ServiceModel.Diagnostics.TraceXPathNavigator::current
    IL_0008:  brtrue.s   IL_000e

    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  br.s       IL_004d

    IL_000e:  ldarg.0
    IL_000f:  ldfld      bool System.ServiceModel.Diagnostics.TraceXPathNavigator::closed
    IL_0014:  brfalse.s  IL_001a

    IL_0016:  ldc.i4.5
    IL_0017:  stloc.0
    IL_0018:  br.s       IL_004d

    IL_001a:  ldarg.0
    IL_001b:  ldfld      valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::state
    IL_0020:  stloc.1
    IL_0021:  ldloc.1
    IL_0022:  ldc.i4.1
    IL_0023:  sub
    IL_0024:  switch     ( 
                          IL_0043,
                          IL_003f,
                          IL_004d,
                          IL_0047)
    IL_0039:  ldloc.1
    IL_003a:  ldc.i4.8
    IL_003b:  beq.s      IL_004b

    IL_003d:  br.s       IL_004d

    IL_003f:  ldc.i4.3
    IL_0040:  stloc.0
    IL_0041:  br.s       IL_004d

    IL_0043:  ldc.i4.2
    IL_0044:  stloc.0
    IL_0045:  br.s       IL_004d

    IL_0047:  ldc.i4.4
    IL_0048:  stloc.0
    IL_0049:  br.s       IL_004d

    IL_004b:  ldc.i4.4
    IL_004c:  stloc.0
    IL_004d:  ldloc.0
    IL_004e:  ret
  } // end of method TraceXPathNavigator::get_WriteState

  .method public hidebysig virtual instance string 
          ToString() cil managed
  {
    // Code size       44 (0x2c)
    .maxstack  3
    .locals init (class [mscorlib]System.Text.StringBuilder V_0,
             class System.ServiceModel.Diagnostics.EncodingFallbackAwareXmlTextWriter V_1)
    IL_0000:  ldarg.0
    IL_0001:  callvirt   instance void [System.Xml]System.Xml.XPath.XPathNavigator::MoveToRoot()
    IL_0006:  newobj     instance void [mscorlib]System.Text.StringBuilder::.ctor()
    IL_000b:  stloc.0
    IL_000c:  ldloc.0
    IL_000d:  call       class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_CurrentCulture()
    IL_0012:  newobj     instance void [mscorlib]System.IO.StringWriter::.ctor(class [mscorlib]System.Text.StringBuilder,
                                                                               class [mscorlib]System.IFormatProvider)
    IL_0017:  newobj     instance void System.ServiceModel.Diagnostics.EncodingFallbackAwareXmlTextWriter::.ctor(class [mscorlib]System.IO.TextWriter)
    IL_001c:  stloc.1
    IL_001d:  ldloc.1
    IL_001e:  ldarg.0
    IL_001f:  ldc.i4.0
    IL_0020:  callvirt   instance void [System.Xml]System.Xml.XmlWriter::WriteNode(class [System.Xml]System.Xml.XPath.XPathNavigator,
                                                                                   bool)
    IL_0025:  ldloc.0
    IL_0026:  callvirt   instance string [mscorlib]System.Object::ToString()
    IL_002b:  ret
  } // end of method TraceXPathNavigator::ToString

  .method private hidebysig instance void 
          VerifySize(class System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable node) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  callvirt   instance int32 System.ServiceModel.Diagnostics.TraceXPathNavigator/IMeasurable::get_Size()
    IL_0007:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::VerifySize(int32)
    IL_000c:  ret
  } // end of method TraceXPathNavigator::VerifySize

  .method private hidebysig instance void 
          VerifySize(string node) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  callvirt   instance int32 [mscorlib]System.String::get_Length()
    IL_0007:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::VerifySize(int32)
    IL_000c:  ret
  } // end of method TraceXPathNavigator::VerifySize

  .method private hidebysig instance void 
          VerifySize(int32 nodeSize) cil managed
  {
    // Code size       49 (0x31)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator::maxSize
    IL_0006:  ldc.i4.m1
    IL_0007:  beq.s      IL_0021

    IL_0009:  ldarg.0
    IL_000a:  ldfld      int64 System.ServiceModel.Diagnostics.TraceXPathNavigator::currentSize
    IL_000f:  ldarg.1
    IL_0010:  conv.i8
    IL_0011:  add
    IL_0012:  ldarg.0
    IL_0013:  ldfld      int32 System.ServiceModel.Diagnostics.TraceXPathNavigator::maxSize
    IL_0018:  conv.i8
    IL_0019:  ble.s      IL_0021

    IL_001b:  newobj     instance void System.ServiceModel.Diagnostics.PlainXmlWriter/MaxSizeExceededException::.ctor()
    IL_0020:  throw

    IL_0021:  ldarg.0
    IL_0022:  ldarg.0
    IL_0023:  ldfld      int64 System.ServiceModel.Diagnostics.TraceXPathNavigator::currentSize
    IL_0028:  ldarg.1
    IL_0029:  conv.i8
    IL_002a:  add
    IL_002b:  stfld      int64 System.ServiceModel.Diagnostics.TraceXPathNavigator::currentSize
    IL_0030:  ret
  } // end of method TraceXPathNavigator::VerifySize

  .method public hidebysig instance void 
          RemovePii(string[][] paths) cil managed
  {
    // Code size       42 (0x2a)
    .maxstack  2
    .locals init (string[][] V_0,
             int32 V_1,
             string[] V_2)
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000e

    IL_0003:  ldstr      "paths"
    IL_0008:  newobj     instance void [mscorlib]System.ArgumentNullException::.ctor(string)
    IL_000d:  throw

    IL_000e:  ldarg.1
    IL_000f:  stloc.0
    IL_0010:  ldc.i4.0
    IL_0011:  stloc.1
    IL_0012:  br.s       IL_0023

    IL_0014:  ldloc.0
    IL_0015:  ldloc.1
    IL_0016:  ldelem.ref
    IL_0017:  stloc.2
    IL_0018:  ldarg.0
    IL_0019:  ldloc.2
    IL_001a:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::RemovePii(string[])
    IL_001f:  ldloc.1
    IL_0020:  ldc.i4.1
    IL_0021:  add
    IL_0022:  stloc.1
    IL_0023:  ldloc.1
    IL_0024:  ldloc.0
    IL_0025:  ldlen
    IL_0026:  conv.i4
    IL_0027:  blt.s      IL_0014

    IL_0029:  ret
  } // end of method TraceXPathNavigator::RemovePii

  .method public hidebysig instance void 
          RemovePii(string[] path) cil managed
  {
    // Code size       13 (0xd)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldsfld     string[] System.ServiceModel.Diagnostics.DiagnosticStrings::PiiList
    IL_0007:  call       instance void System.ServiceModel.Diagnostics.TraceXPathNavigator::RemovePii(string[],
                                                                                                      string[])
    IL_000c:  ret
  } // end of method TraceXPathNavigator::RemovePii

  .method public hidebysig instance void 
          RemovePii(string[] headersPath,
                    string[] piiList) cil managed
  {
    // Code size       69 (0x45)
    .maxstack  2
    .locals init (class [mscorlib]System.Collections.Generic.IEnumerator`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode> V_0,
             class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode V_1)
    IL_0000:  ldarg.0
    IL_0001:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::root
    IL_0006:  brtrue.s   IL_000e

    IL_0008:  newobj     instance void [mscorlib]System.InvalidOperationException::.ctor()
    IL_000d:  throw

    IL_000e:  ldarg.0
    IL_000f:  ldfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::root
    IL_0014:  ldarg.1
    IL_0015:  callvirt   instance class [mscorlib]System.Collections.Generic.IEnumerable`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::FindSubnodes(string[])
    IL_001a:  callvirt   instance class [mscorlib]System.Collections.Generic.IEnumerator`1<!0> class [mscorlib]System.Collections.Generic.IEnumerable`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode>::GetEnumerator()
    IL_001f:  stloc.0
    .try
    {
      IL_0020:  br.s       IL_0030

      IL_0022:  ldloc.0
      IL_0023:  callvirt   instance !0 class [mscorlib]System.Collections.Generic.IEnumerator`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode>::get_Current()
      IL_0028:  stloc.1
      IL_0029:  ldloc.1
      IL_002a:  ldarg.2
      IL_002b:  call       void System.ServiceModel.Diagnostics.TraceXPathNavigator::MaskSubnodes(class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode,
                                                                                                  string[])
      IL_0030:  ldloc.0
      IL_0031:  callvirt   instance bool [mscorlib]System.Collections.IEnumerator::MoveNext()
      IL_0036:  brtrue.s   IL_0022

      IL_0038:  leave.s    IL_0044

    }  // end .try
    finally
    {
      IL_003a:  ldloc.0
      IL_003b:  brfalse.s  IL_0043

      IL_003d:  ldloc.0
      IL_003e:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_0043:  endfinally
    }  // end handler
    IL_0044:  ret
  } // end of method TraceXPathNavigator::RemovePii

  .method private hidebysig static void  MaskElement(class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode element) cil managed
  {
    // Code size       46 (0x2e)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  brfalse.s  IL_002d

    IL_0003:  ldarg.0
    IL_0004:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
    IL_0009:  callvirt   instance void class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::Clear()
    IL_000e:  ldarg.0
    IL_000f:  ldstr      "Removed"
    IL_0014:  ldarg.0
    IL_0015:  newobj     instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode::.ctor(string,
                                                                                                              class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode)
    IL_001a:  callvirt   instance void System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::Add(class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode)
    IL_001f:  ldarg.0
    IL_0020:  ldnull
    IL_0021:  stfld      class System.ServiceModel.Diagnostics.TraceXPathNavigator/TextNode System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::text
    IL_0026:  ldarg.0
    IL_0027:  ldnull
    IL_0028:  stfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/AttributeNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::attributes
    IL_002d:  ret
  } // end of method TraceXPathNavigator::MaskElement

  .method private hidebysig static void  MaskSubnodes(class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode element,
                                                      string[] elementNames) cil managed
  {
    // Code size       9 (0x9)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  ldc.i4.0
    IL_0003:  call       void System.ServiceModel.Diagnostics.TraceXPathNavigator::MaskSubnodes(class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode,
                                                                                                string[],
                                                                                                bool)
    IL_0008:  ret
  } // end of method TraceXPathNavigator::MaskSubnodes

  .method private hidebysig static void  MaskSubnodes(class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode element,
                                                      string[] elementNames,
                                                      bool processNodeItself) cil managed
  {
    // Code size       141 (0x8d)
    .maxstack  3
    .locals init (bool V_0,
             string[] V_1,
             int32 V_2,
             string V_3,
             valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> V_4,
             class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode V_5)
    IL_0000:  ldarg.1
    IL_0001:  brtrue.s   IL_000e

    IL_0003:  ldstr      "elementNames"
    IL_0008:  newobj     instance void [mscorlib]System.ArgumentNullException::.ctor(string)
    IL_000d:  throw

    IL_000e:  ldarg.0
    IL_000f:  brfalse.s  IL_008c

    IL_0011:  ldc.i4.1
    IL_0012:  stloc.0
    IL_0013:  ldarg.2
    IL_0014:  brfalse.s  IL_0042

    IL_0016:  ldarg.1
    IL_0017:  stloc.1
    IL_0018:  ldc.i4.0
    IL_0019:  stloc.2
    IL_001a:  br.s       IL_003c

    IL_001c:  ldloc.1
    IL_001d:  ldloc.2
    IL_001e:  ldelem.ref
    IL_001f:  stloc.3
    IL_0020:  ldloc.3
    IL_0021:  ldarg.0
    IL_0022:  ldfld      string System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::name
    IL_0027:  call       int32 [mscorlib]System.String::CompareOrdinal(string,
                                                                       string)
    IL_002c:  brtrue.s   IL_0038

    IL_002e:  ldarg.0
    IL_002f:  call       void System.ServiceModel.Diagnostics.TraceXPathNavigator::MaskElement(class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode)
    IL_0034:  ldc.i4.0
    IL_0035:  stloc.0
    IL_0036:  br.s       IL_0042

    IL_0038:  ldloc.2
    IL_0039:  ldc.i4.1
    IL_003a:  add
    IL_003b:  stloc.2
    IL_003c:  ldloc.2
    IL_003d:  ldloc.1
    IL_003e:  ldlen
    IL_003f:  conv.i4
    IL_0040:  blt.s      IL_001c

    IL_0042:  ldloc.0
    IL_0043:  brfalse.s  IL_008c

    IL_0045:  ldarg.0
    IL_0046:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
    IL_004b:  brfalse.s  IL_008c

    IL_004d:  ldarg.0
    IL_004e:  ldfld      class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode> System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode::childNodes
    IL_0053:  callvirt   instance valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<!0> class [mscorlib]System.Collections.Generic.List`1<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::GetEnumerator()
    IL_0058:  stloc.s    V_4
    .try
    {
      IL_005a:  br.s       IL_0073

      IL_005c:  ldloca.s   V_4
      IL_005e:  call       instance !0 valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::get_Current()
      IL_0063:  castclass  System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode
      IL_0068:  stloc.s    V_5
      IL_006a:  ldloc.s    V_5
      IL_006c:  ldarg.1
      IL_006d:  ldc.i4.1
      IL_006e:  call       void System.ServiceModel.Diagnostics.TraceXPathNavigator::MaskSubnodes(class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode,
                                                                                                  string[],
                                                                                                  bool)
      IL_0073:  ldloca.s   V_4
      IL_0075:  call       instance bool valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>::MoveNext()
      IL_007a:  brtrue.s   IL_005c

      IL_007c:  leave.s    IL_008c

    }  // end .try
    finally
    {
      IL_007e:  ldloca.s   V_4
      IL_0080:  constrained. valuetype [mscorlib]System.Collections.Generic.List`1/Enumerator<class System.ServiceModel.Diagnostics.TraceXPathNavigator/TraceNode>
      IL_0086:  callvirt   instance void [mscorlib]System.IDisposable::Dispose()
      IL_008b:  endfinally
    }  // end handler
    IL_008c:  ret
  } // end of method TraceXPathNavigator::MaskSubnodes

  .property instance string BaseURI()
  {
    .get instance string System.ServiceModel.Diagnostics.TraceXPathNavigator::get_BaseURI()
  } // end of property TraceXPathNavigator::BaseURI
  .property instance bool IsEmptyElement()
  {
    .get instance bool System.ServiceModel.Diagnostics.TraceXPathNavigator::get_IsEmptyElement()
  } // end of property TraceXPathNavigator::IsEmptyElement
  .property instance string LocalName()
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerDisplayAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
    .get instance string System.ServiceModel.Diagnostics.TraceXPathNavigator::get_LocalName()
  } // end of property TraceXPathNavigator::LocalName
  .property instance string Name()
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerDisplayAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
    .get instance string System.ServiceModel.Diagnostics.TraceXPathNavigator::get_Name()
  } // end of property TraceXPathNavigator::Name
  .property instance class [System.Xml]System.Xml.XmlNameTable
          NameTable()
  {
    .get instance class [System.Xml]System.Xml.XmlNameTable System.ServiceModel.Diagnostics.TraceXPathNavigator::get_NameTable()
  } // end of property TraceXPathNavigator::NameTable
  .property instance string NamespaceURI()
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerDisplayAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
    .get instance string System.ServiceModel.Diagnostics.TraceXPathNavigator::get_NamespaceURI()
  } // end of property TraceXPathNavigator::NamespaceURI
  .property instance valuetype [System.Xml]System.Xml.XPath.XPathNodeType
          NodeType()
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerDisplayAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
    .get instance valuetype [System.Xml]System.Xml.XPath.XPathNodeType System.ServiceModel.Diagnostics.TraceXPathNavigator::get_NodeType()
  } // end of property TraceXPathNavigator::NodeType
  .property instance string Prefix()
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerDisplayAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
    .get instance string System.ServiceModel.Diagnostics.TraceXPathNavigator::get_Prefix()
  } // end of property TraceXPathNavigator::Prefix
  .property instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode
          CurrentComment()
  {
    .get instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/CommentNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentComment()
  } // end of property TraceXPathNavigator::CurrentComment
  .property instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode
          CurrentElement()
  {
    .get instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ElementNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentElement()
  } // end of property TraceXPathNavigator::CurrentElement
  .property instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode
          CurrentProcessingInstruction()
  {
    .get instance class System.ServiceModel.Diagnostics.TraceXPathNavigator/ProcessingInstructionNode System.ServiceModel.Diagnostics.TraceXPathNavigator::get_CurrentProcessingInstruction()
  } // end of property TraceXPathNavigator::CurrentProcessingInstruction
  .property instance string Value()
  {
    .custom instance void [mscorlib]System.Diagnostics.DebuggerDisplayAttribute::.ctor(string) = ( 01 00 00 00 00 ) 
    .get instance string System.ServiceModel.Diagnostics.TraceXPathNavigator::get_Value()
  } // end of property TraceXPathNavigator::Value
  .property instance valuetype [System.Xml]System.Xml.WriteState
          WriteState()
  {
    .get instance valuetype [System.Xml]System.Xml.WriteState System.ServiceModel.Diagnostics.TraceXPathNavigator::get_WriteState()
  } // end of property TraceXPathNavigator::WriteState
} // end of class System.ServiceModel.Diagnostics.TraceXPathNavigator

.class private auto ansi beforefieldinit System.ServiceModel.Diagnostics.EncodingFallbackAwareXmlTextWriter
       extends [System.Xml]System.Xml.XmlTextWriter
{
  .field private class [mscorlib]System.Text.Encoding encoding
  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(class [mscorlib]System.IO.TextWriter writer) cil managed
  {
    // Code size       20 (0x14)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void [System.Xml]System.Xml.XmlTextWriter::.ctor(class [mscorlib]System.IO.TextWriter)
    IL_0007:  ldarg.0
    IL_0008:  ldarg.1
    IL_0009:  callvirt   instance class [mscorlib]System.Text.Encoding [mscorlib]System.IO.TextWriter::get_Encoding()
    IL_000e:  stfld      class [mscorlib]System.Text.Encoding System.ServiceModel.Diagnostics.EncodingFallbackAwareXmlTextWriter::encoding
    IL_0013:  ret
  } // end of method EncodingFallbackAwareXmlTextWriter::.ctor

  .method public hidebysig virtual instance void 
          WriteString(string 'value') cil managed
  {
    // Code size       52 (0x34)
    .maxstack  2
    .locals init (uint8[] V_0)
    IL_0000:  ldarg.1
    IL_0001:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
    IL_0006:  brtrue.s   IL_002c

    IL_0008:  ldarg.0
    IL_0009:  ldarg.1
    IL_000a:  call       instance bool System.ServiceModel.Diagnostics.EncodingFallbackAwareXmlTextWriter::ContainsInvalidXmlChar(string)
    IL_000f:  brfalse.s  IL_002c

    IL_0011:  ldarg.0
    IL_0012:  ldfld      class [mscorlib]System.Text.Encoding System.ServiceModel.Diagnostics.EncodingFallbackAwareXmlTextWriter::encoding
    IL_0017:  ldarg.1
    IL_0018:  callvirt   instance uint8[] [mscorlib]System.Text.Encoding::GetBytes(string)
    IL_001d:  stloc.0
    IL_001e:  ldarg.0
    IL_001f:  ldfld      class [mscorlib]System.Text.Encoding System.ServiceModel.Diagnostics.EncodingFallbackAwareXmlTextWriter::encoding
    IL_0024:  ldloc.0
    IL_0025:  callvirt   instance string [mscorlib]System.Text.Encoding::GetString(uint8[])
    IL_002a:  starg.s    'value'
    IL_002c:  ldarg.0
    IL_002d:  ldarg.1
    IL_002e:  call       instance void [System.Xml]System.Xml.XmlTextWriter::WriteString(string)
    IL_0033:  ret
  } // end of method EncodingFallbackAwareXmlTextWriter::WriteString

  .method private hidebysig instance bool 
          ContainsInvalidXmlChar(string 'value') cil managed
  {
    // Code size       84 (0x54)
    .maxstack  3
    .locals init (int32 V_0,
             int32 V_1)
    IL_0000:  ldarg.1
    IL_0001:  call       bool [mscorlib]System.String::IsNullOrEmpty(string)
    IL_0006:  brfalse.s  IL_000a

    IL_0008:  ldc.i4.0
    IL_0009:  ret

    IL_000a:  ldc.i4.0
    IL_000b:  stloc.0
    IL_000c:  ldarg.1
    IL_000d:  callvirt   instance int32 [mscorlib]System.String::get_Length()
    IL_0012:  stloc.1
    IL_0013:  br.s       IL_004e

    IL_0015:  ldarg.1
    IL_0016:  ldloc.0
    IL_0017:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
    IL_001c:  call       bool [System.Xml]System.Xml.XmlConvert::IsXmlChar(char)
    IL_0021:  brfalse.s  IL_0029

    IL_0023:  ldloc.0
    IL_0024:  ldc.i4.1
    IL_0025:  add
    IL_0026:  stloc.0
    IL_0027:  br.s       IL_004e

    IL_0029:  ldloc.0
    IL_002a:  ldc.i4.1
    IL_002b:  add
    IL_002c:  ldloc.1
    IL_002d:  bge.s      IL_004c

    IL_002f:  ldarg.1
    IL_0030:  ldloc.0
    IL_0031:  ldc.i4.1
    IL_0032:  add
    IL_0033:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
    IL_0038:  ldarg.1
    IL_0039:  ldloc.0
    IL_003a:  callvirt   instance char [mscorlib]System.String::get_Chars(int32)
    IL_003f:  call       bool [System.Xml]System.Xml.XmlConvert::IsXmlSurrogatePair(char,
                                                                                    char)
    IL_0044:  brfalse.s  IL_004c

    IL_0046:  ldloc.0
    IL_0047:  ldc.i4.2
    IL_0048:  add
    IL_0049:  stloc.0
    IL_004a:  br.s       IL_004e

    IL_004c:  ldc.i4.1
    IL_004d:  ret

    IL_004e:  ldloc.0
    IL_004f:  ldloc.1
    IL_0050:  blt.s      IL_0015

    IL_0052:  ldc.i4.0
    IL_0053:  ret
  } // end of method EncodingFallbackAwareXmlTextWriter::ContainsInvalidXmlChar

} // end of class System.ServiceModel.Diagnostics.EncodingFallbackAwareXmlTextWriter

.class private auto ansi beforefieldinit System.ServiceModel.Diagnostics.Utility
       extends [mscorlib]System.Object
{
  .field private class System.ServiceModel.Diagnostics.ExceptionUtility exceptionUtility
  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor(class System.ServiceModel.Diagnostics.ExceptionUtility exceptionUtility) cil managed
  {
    .custom instance void [mscorlib]System.ObsoleteAttribute::.ctor(string) = ( 01 00 46 46 6F 72 20 53 4D 44 69 61 67 6E 6F 73   // ..FFor SMDiagnos
                                                                                74 69 63 73 2E 64 6C 6C 20 75 73 65 20 6F 6E 6C   // tics.dll use onl
                                                                                79 2E 20 43 61 6C 6C 20 44 69 61 67 6E 6F 73 74   // y. Call Diagnost
                                                                                69 63 55 74 69 6C 69 74 79 2E 55 74 69 6C 69 74   // icUtility.Utilit
                                                                                79 20 69 6E 73 74 65 61 64 00 00 )                // y instead..
    // Code size       14 (0xe)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldarg.1
    IL_0008:  stfld      class System.ServiceModel.Diagnostics.ExceptionUtility System.ServiceModel.Diagnostics.Utility::exceptionUtility
    IL_000d:  ret
  } // end of method Utility::.ctor

  .method assembly hidebysig static void 
          CloseInvalidOutSafeHandle(class [mscorlib]System.Runtime.InteropServices.SafeHandle handle) cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  brfalse.s  IL_0009

    IL_0003:  ldarg.0
    IL_0004:  callvirt   instance void [mscorlib]System.Runtime.InteropServices.SafeHandle::SetHandleAsInvalid()
    IL_0009:  ret
  } // end of method Utility::CloseInvalidOutSafeHandle

  .method assembly hidebysig static void 
          CloseInvalidOutCriticalHandle(class [mscorlib]System.Runtime.InteropServices.CriticalHandle handle) cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  brfalse.s  IL_0009

    IL_0003:  ldarg.0
    IL_0004:  callvirt   instance void [mscorlib]System.Runtime.InteropServices.CriticalHandle::SetHandleAsInvalid()
    IL_0009:  ret
  } // end of method Utility::CloseInvalidOutCriticalHandle

  .method assembly hidebysig instance valuetype [mscorlib]System.Guid 
          CreateGuid(string guidString) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  call       valuetype [mscorlib]System.Guid [System.ServiceModel.Internals]System.Runtime.Fx::CreateGuid(string)
    IL_0006:  ret
  } // end of method Utility::CreateGuid

  .method assembly hidebysig instance bool 
          TryCreateGuid(string guidString,
                        [out] valuetype [mscorlib]System.Guid& result) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  ldarg.2
    IL_0002:  call       bool [System.ServiceModel.Internals]System.Runtime.Fx::TryCreateGuid(string,
                                                                                              valuetype [mscorlib]System.Guid&)
    IL_0007:  ret
  } // end of method Utility::TryCreateGuid

  .method assembly hidebysig instance uint8[] 
          AllocateByteArray(int32 size) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  call       uint8[] [System.ServiceModel.Internals]System.Runtime.Fx::AllocateByteArray(int32)
    IL_0006:  ret
  } // end of method Utility::AllocateByteArray

  .method assembly hidebysig instance char[] 
          AllocateCharArray(int32 size) cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  call       char[] [System.ServiceModel.Internals]System.Runtime.Fx::AllocateCharArray(int32)
    IL_0006:  ret
  } // end of method Utility::AllocateCharArray

} // end of class System.ServiceModel.Diagnostics.Utility

.class private auto ansi sealed beforefieldinit System.ServiceModel.Diagnostics.TraceSRDescriptionAttribute
       extends [System]System.ComponentModel.DescriptionAttribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 FF 7F 00 00 00 00 ) 
  .field private bool replaced
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string description) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void [System]System.ComponentModel.DescriptionAttribute::.ctor(string)
    IL_0007:  ret
  } // end of method TraceSRDescriptionAttribute::.ctor

  .method public hidebysig specialname virtual 
          instance string  get_Description() cil managed
  {
    // Code size       39 (0x27)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldfld      bool System.ServiceModel.Diagnostics.TraceSRDescriptionAttribute::replaced
    IL_0006:  brtrue.s   IL_0020

    IL_0008:  ldarg.0
    IL_0009:  ldc.i4.1
    IL_000a:  stfld      bool System.ServiceModel.Diagnostics.TraceSRDescriptionAttribute::replaced
    IL_000f:  ldarg.0
    IL_0010:  ldarg.0
    IL_0011:  call       instance string [System]System.ComponentModel.DescriptionAttribute::get_Description()
    IL_0016:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
    IL_001b:  call       instance void [System]System.ComponentModel.DescriptionAttribute::set_DescriptionValue(string)
    IL_0020:  ldarg.0
    IL_0021:  call       instance string [System]System.ComponentModel.DescriptionAttribute::get_Description()
    IL_0026:  ret
  } // end of method TraceSRDescriptionAttribute::get_Description

  .property instance string Description()
  {
    .get instance string System.ServiceModel.Diagnostics.TraceSRDescriptionAttribute::get_Description()
  } // end of property TraceSRDescriptionAttribute::Description
} // end of class System.ServiceModel.Diagnostics.TraceSRDescriptionAttribute

.class private auto ansi sealed beforefieldinit System.ServiceModel.Diagnostics.TraceSRCategoryAttribute
       extends [System]System.ComponentModel.CategoryAttribute
{
  .custom instance void [mscorlib]System.AttributeUsageAttribute::.ctor(valuetype [mscorlib]System.AttributeTargets) = ( 01 00 FF 7F 00 00 00 00 ) 
  .method public hidebysig specialname rtspecialname 
          instance void  .ctor(string category) cil managed
  {
    // Code size       8 (0x8)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  ldarg.1
    IL_0002:  call       instance void [System]System.ComponentModel.CategoryAttribute::.ctor(string)
    IL_0007:  ret
  } // end of method TraceSRCategoryAttribute::.ctor

  .method family hidebysig virtual instance string 
          GetLocalizedString(string 'value') cil managed
  {
    // Code size       7 (0x7)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
    IL_0006:  ret
  } // end of method TraceSRCategoryAttribute::GetLocalizedString

} // end of class System.ServiceModel.Diagnostics.TraceSRCategoryAttribute

.class private auto ansi sealed beforefieldinit System.ServiceModel.Diagnostics.TraceSR
       extends [mscorlib]System.Object
{
  .field static assembly literal string ActivityBoundary = "ActivityBoundary"
  .field static assembly literal string ThrowingException = "ThrowingException"
  .field static assembly literal string TraceHandledException = "TraceHandledException"
  .field static assembly literal string TraceCodeAppDomainUnload = "TraceCodeAppDomainUnload"
  .field static assembly literal string TraceCodeEventLog = "TraceCodeEventLog"
  .field static assembly literal string TraceCodeTraceTruncatedQuotaExceeded = "TraceCodeTraceTruncatedQuotaExceeded"
  .field static assembly literal string UnhandledException = "UnhandledException"
  .field static assembly literal string WriteCharsInvalidContent = "WriteCharsInvalidContent"
  .field static assembly literal string GenericCallbackException = "GenericCallbackException"
  .field static assembly literal string StringNullOrEmpty = "StringNullOrEmpty"
  .field private static class System.ServiceModel.Diagnostics.TraceSR loader
  .field private class [mscorlib]System.Resources.ResourceManager resources
  .method assembly hidebysig specialname rtspecialname 
          instance void  .ctor() cil managed
  {
    // Code size       34 (0x22)
    .maxstack  8
    IL_0000:  ldarg.0
    IL_0001:  call       instance void [mscorlib]System.Object::.ctor()
    IL_0006:  ldarg.0
    IL_0007:  ldstr      "SMDiagnostics"
    IL_000c:  ldarg.0
    IL_000d:  call       instance class [mscorlib]System.Type [mscorlib]System.Object::GetType()
    IL_0012:  callvirt   instance class [mscorlib]System.Reflection.Assembly [mscorlib]System.Type::get_Assembly()
    IL_0017:  newobj     instance void [mscorlib]System.Resources.ResourceManager::.ctor(string,
                                                                                         class [mscorlib]System.Reflection.Assembly)
    IL_001c:  stfld      class [mscorlib]System.Resources.ResourceManager System.ServiceModel.Diagnostics.TraceSR::resources
    IL_0021:  ret
  } // end of method TraceSR::.ctor

  .method private hidebysig static class System.ServiceModel.Diagnostics.TraceSR 
          GetLoader() cil managed
  {
    // Code size       32 (0x20)
    .maxstack  3
    .locals init (class System.ServiceModel.Diagnostics.TraceSR V_0)
    IL_0000:  ldsfld     class System.ServiceModel.Diagnostics.TraceSR System.ServiceModel.Diagnostics.TraceSR::loader
    IL_0005:  brtrue.s   IL_001a

    IL_0007:  newobj     instance void System.ServiceModel.Diagnostics.TraceSR::.ctor()
    IL_000c:  stloc.0
    IL_000d:  ldsflda    class System.ServiceModel.Diagnostics.TraceSR System.ServiceModel.Diagnostics.TraceSR::loader
    IL_0012:  ldloc.0
    IL_0013:  ldnull
    IL_0014:  call       !!0 [mscorlib]System.Threading.Interlocked::CompareExchange<class System.ServiceModel.Diagnostics.TraceSR>(!!0&,
                                                                                                                                    !!0,
                                                                                                                                    !!0)
    IL_0019:  pop
    IL_001a:  ldsfld     class System.ServiceModel.Diagnostics.TraceSR System.ServiceModel.Diagnostics.TraceSR::loader
    IL_001f:  ret
  } // end of method TraceSR::GetLoader

  .method private hidebysig specialname static 
          class [mscorlib]System.Globalization.CultureInfo 
          get_Culture() cil managed
  {
    // Code size       2 (0x2)
    .maxstack  8
    IL_0000:  ldnull
    IL_0001:  ret
  } // end of method TraceSR::get_Culture

  .method public hidebysig specialname static 
          class [mscorlib]System.Resources.ResourceManager 
          get_Resources() cil managed
  {
    // Code size       11 (0xb)
    .maxstack  8
    IL_0000:  call       class System.ServiceModel.Diagnostics.TraceSR System.ServiceModel.Diagnostics.TraceSR::GetLoader()
    IL_0005:  ldfld      class [mscorlib]System.Resources.ResourceManager System.ServiceModel.Diagnostics.TraceSR::resources
    IL_000a:  ret
  } // end of method TraceSR::get_Resources

  .method public hidebysig static string 
          GetString(string name,
                    object[] args) cil managed
  {
    .param [2]
    .custom instance void [mscorlib]System.ParamArrayAttribute::.ctor() = ( 01 00 00 00 ) 
    // Code size       115 (0x73)
    .maxstack  5
    .locals init (class System.ServiceModel.Diagnostics.TraceSR V_0,
             string V_1,
             int32 V_2,
             string V_3)
    IL_0000:  call       class System.ServiceModel.Diagnostics.TraceSR System.ServiceModel.Diagnostics.TraceSR::GetLoader()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  brtrue.s   IL_000b

    IL_0009:  ldnull
    IL_000a:  ret

    IL_000b:  ldloc.0
    IL_000c:  ldfld      class [mscorlib]System.Resources.ResourceManager System.ServiceModel.Diagnostics.TraceSR::resources
    IL_0011:  ldarg.0
    IL_0012:  call       class [mscorlib]System.Globalization.CultureInfo System.ServiceModel.Diagnostics.TraceSR::get_Culture()
    IL_0017:  callvirt   instance string [mscorlib]System.Resources.ResourceManager::GetString(string,
                                                                                               class [mscorlib]System.Globalization.CultureInfo)
    IL_001c:  stloc.1
    IL_001d:  ldarg.1
    IL_001e:  brfalse.s  IL_0071

    IL_0020:  ldarg.1
    IL_0021:  ldlen
    IL_0022:  brfalse.s  IL_0071

    IL_0024:  ldc.i4.0
    IL_0025:  stloc.2
    IL_0026:  br.s       IL_005e

    IL_0028:  ldarg.1
    IL_0029:  ldloc.2
    IL_002a:  ldelem.ref
    IL_002b:  isinst     [mscorlib]System.String
    IL_0030:  stloc.3
    IL_0031:  ldloc.3
    IL_0032:  brfalse.s  IL_005a

    IL_0034:  ldloc.3
    IL_0035:  callvirt   instance int32 [mscorlib]System.String::get_Length()
    IL_003a:  ldc.i4     0x400
    IL_003f:  ble.s      IL_005a

    IL_0041:  ldarg.1
    IL_0042:  ldloc.2
    IL_0043:  ldloc.3
    IL_0044:  ldc.i4.0
    IL_0045:  ldc.i4     0x3fd
    IL_004a:  callvirt   instance string [mscorlib]System.String::Substring(int32,
                                                                            int32)
    IL_004f:  ldstr      "..."
    IL_0054:  call       string [mscorlib]System.String::Concat(string,
                                                                string)
    IL_0059:  stelem.ref
    IL_005a:  ldloc.2
    IL_005b:  ldc.i4.1
    IL_005c:  add
    IL_005d:  stloc.2
    IL_005e:  ldloc.2
    IL_005f:  ldarg.1
    IL_0060:  ldlen
    IL_0061:  conv.i4
    IL_0062:  blt.s      IL_0028

    IL_0064:  call       class [mscorlib]System.Globalization.CultureInfo [mscorlib]System.Globalization.CultureInfo::get_CurrentCulture()
    IL_0069:  ldloc.1
    IL_006a:  ldarg.1
    IL_006b:  call       string [mscorlib]System.String::Format(class [mscorlib]System.IFormatProvider,
                                                                string,
                                                                object[])
    IL_0070:  ret

    IL_0071:  ldloc.1
    IL_0072:  ret
  } // end of method TraceSR::GetString

  .method public hidebysig static string 
          GetString(string name) cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  3
    .locals init (class System.ServiceModel.Diagnostics.TraceSR V_0)
    IL_0000:  call       class System.ServiceModel.Diagnostics.TraceSR System.ServiceModel.Diagnostics.TraceSR::GetLoader()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  brtrue.s   IL_000b

    IL_0009:  ldnull
    IL_000a:  ret

    IL_000b:  ldloc.0
    IL_000c:  ldfld      class [mscorlib]System.Resources.ResourceManager System.ServiceModel.Diagnostics.TraceSR::resources
    IL_0011:  ldarg.0
    IL_0012:  call       class [mscorlib]System.Globalization.CultureInfo System.ServiceModel.Diagnostics.TraceSR::get_Culture()
    IL_0017:  callvirt   instance string [mscorlib]System.Resources.ResourceManager::GetString(string,
                                                                                               class [mscorlib]System.Globalization.CultureInfo)
    IL_001c:  ret
  } // end of method TraceSR::GetString

  .method public hidebysig static string 
          GetString(string name,
                    [out] bool& usedFallback) cil managed
  {
    // Code size       10 (0xa)
    .maxstack  8
    IL_0000:  ldarg.1
    IL_0001:  ldc.i4.0
    IL_0002:  stind.i1
    IL_0003:  ldarg.0
    IL_0004:  call       string System.ServiceModel.Diagnostics.TraceSR::GetString(string)
    IL_0009:  ret
  } // end of method TraceSR::GetString

  .method public hidebysig static object 
          GetObject(string name) cil managed
  {
    // Code size       29 (0x1d)
    .maxstack  3
    .locals init (class System.ServiceModel.Diagnostics.TraceSR V_0)
    IL_0000:  call       class System.ServiceModel.Diagnostics.TraceSR System.ServiceModel.Diagnostics.TraceSR::GetLoader()
    IL_0005:  stloc.0
    IL_0006:  ldloc.0
    IL_0007:  brtrue.s   IL_000b

    IL_0009:  ldnull
    IL_000a:  ret

    IL_000b:  ldloc.0
    IL_000c:  ldfld      class [mscorlib]System.Resources.ResourceManager System.ServiceModel.Diagnostics.TraceSR::resources
    IL_0011:  ldarg.0
    IL_0012:  call       class [mscorlib]System.Globalization.CultureInfo System.ServiceModel.Diagnostics.TraceSR::get_Culture()
    IL_0017:  callvirt   instance object [mscorlib]System.Resources.ResourceManager::GetObject(string,
                                                                                               class [mscorlib]System.Globalization.CultureInfo)
    IL_001c:  ret
  } // end of method TraceSR::GetObject

  .property class [mscorlib]System.Globalization.CultureInfo
          Culture()
  {
    .get class [mscorlib]System.Globalization.CultureInfo System.ServiceModel.Diagnostics.TraceSR::get_Culture()
  } // end of property TraceSR::Culture
  .property class [mscorlib]System.Resources.ResourceManager
          Resources()
  {
    .get class [mscorlib]System.Resources.ResourceManager System.ServiceModel.Diagnostics.TraceSR::get_Resources()
  } // end of property TraceSR::Resources
} // end of class System.ServiceModel.Diagnostics.TraceSR


// =============================================================

// *********** DISASSEMBLY COMPLETE ***********************
// WARNING: Created Win32 resource file d:\Repositories\Hartonomous\temp\SMDiagnostics.res
