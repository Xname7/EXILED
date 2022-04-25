// -----------------------------------------------------------------------
// <copyright file="DamagingScp244.cs" company="Exiled Team">
// Copyright (c) Exiled Team. All rights reserved.
// Licensed under the CC BY-SA 3.0 license.
// </copyright>
// -----------------------------------------------------------------------

namespace Exiled.Events.Patches.Events.Scp244
{
#pragma warning disable SA1313
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    using Exiled.API.Features;
    using Exiled.Events.EventArgs;

    using HarmonyLib;

    using InventorySystem;
    using InventorySystem.Items.Usables.Scp244;
    using InventorySystem.Searching;

    using NorthwoodLib.Pools;

    using PlayerStatsSystem;

    using UnityEngine;

    using static HarmonyLib.AccessTools;
    /// <summary>
    /// Patches <see cref="Scp244DeployablePickup.Damage"/> to add missing logic to the <see cref="Scp244DeployablePickup"/>.
    /// </summary>
    [HarmonyPatch(typeof(Scp244DeployablePickup), nameof(Scp244DeployablePickup.Damage))]
    internal static class DamagingScp244Patch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            List<CodeInstruction> newInstructions = ListPool<CodeInstruction>.Shared.Rent(instructions);

            Label returnFalse = generator.DefineLabel();

            Label continueProcessing = generator.DefineLabel();
            Label normalProcessing = generator.DefineLabel();

            int offset = -4;
            int injectionPoint = newInstructions.FindIndex(instruction => instruction.opcode == OpCodes.Sub);
            int index = injectionPoint + offset;

            LocalBuilder exceptionObject = generator.DeclareLocal(typeof(Exception));


            // Our Catch (Try wrapper) block
            ExceptionBlock catchBlock = new ExceptionBlock(ExceptionBlockType.BeginCatchBlock, typeof(Exception));

            // Our Exception handling start
            ExceptionBlock exceptionStart = new ExceptionBlock(ExceptionBlockType.BeginExceptionBlock, typeof(Exception));

            // Our Exception handling end
            ExceptionBlock exceptionEnd = new ExceptionBlock(ExceptionBlockType.EndExceptionBlock);

#pragma warning disable SA1118 // Parameter should not span multiple lines
            newInstructions.InsertRange(index, new[]
            {
                // Load a try wrapper at start
                new CodeInstruction(OpCodes.Nop).WithBlocks(exceptionStart),

                // Load instance of Scp244DeployablePickup EStack[Scp244DeployablePickup Instance]
                new CodeInstruction(OpCodes.Ldarg_0),

                // Load Field (Because of get; set; it's property getter) of instance EStack[Scp244Deployable.State]
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(Scp244DeployablePickup), nameof(Scp244DeployablePickup.State))),

                // Load value 2 (Enum of Scp244State.Destroyed) EStack[Scp244Deployable.State, 2]
                new CodeInstruction(OpCodes.Ldc_I4_2),

                // Jump to return false label EStack[]
                new CodeInstruction(OpCodes.Beq, returnFalse),

                // Continue processing, and load arg 0 (instance) again EStack[Scp244DeployablePickup Instance]
                new CodeInstruction(OpCodes.Ldarg_0),

                // Load arg 1 (param 0) EStack[Scp244DeployablePickup Instance, Float damage]
                new CodeInstruction(OpCodes.Ldarg_1),

                // Load arg 2 (param 1) EStack[Scp244DeployablePickup Instance, Float damage, DamageHandleBase handler]
                new CodeInstruction(OpCodes.Ldarg_2),

                // Pass all 3 variables to DamageScp244 New Object, get a new object in return EStack[DamagingScp244EventArgs Instance]
                new CodeInstruction(OpCodes.Newobj, GetDeclaredConstructors(typeof(DamagingScp244EventArgs))[0]),

                // Copy it for later use again EStack[DamagingScp244EventArgs Instance, DamagingScp244EventArgs Instance]
                new CodeInstruction(OpCodes.Dup),

                // Call Method on Instance EStack[DamagingScp244EventArgs Instance] (pops off so that's why we needed to dup)
                new CodeInstruction(OpCodes.Call, Method(typeof(Handlers.Scp244), nameof(Handlers.Scp244.OnDamagingScp244))),

                // Call its instance field (get; set; so property getter instead of field) EStack[IsAllowed]
                new CodeInstruction(OpCodes.Callvirt, PropertyGetter(typeof(DamagingScp244EventArgs), nameof(DamagingScp244EventArgs.IsAllowed))),

                // If isAllowed = 1, jump to continue route, otherwise, false return occurs below
                new CodeInstruction(OpCodes.Brtrue, continueProcessing),

                // False Route
                new CodeInstruction(OpCodes.Nop).WithLabels(returnFalse),
                new CodeInstruction(OpCodes.Ldc_I4_0),
                new CodeInstruction(OpCodes.Ret),

                // Good route of is allowed being true 
                new CodeInstruction(OpCodes.Nop).WithLabels(continueProcessing),
                new CodeInstruction(OpCodes.Leave_S, normalProcessing),

                // Load generic exception
                new CodeInstruction(OpCodes.Ldloc, exceptionObject),

                // Throw generic
                new CodeInstruction(OpCodes.Throw),

                // Start our catch block
                new CodeInstruction(OpCodes.Nop).WithBlocks(catchBlock),

                // Load the exception from stack
                new CodeInstruction(OpCodes.Stloc, exceptionObject.LocalIndex),

                // Load string with format
                new CodeInstruction(OpCodes.Ldstr, "DamagingScp244Patch failed because of {0}"),

                // Load exception
                new CodeInstruction(OpCodes.Ldloc, exceptionObject.LocalIndex),

                // Call format on string with object to get new string
                new CodeInstruction(OpCodes.Call, Method(typeof(string), nameof(string.Format), new[] { typeof(string), typeof(object) })),

                // Load error
                new CodeInstruction(OpCodes.Call, Method(typeof(Log), nameof(Log.Error), new[] { typeof(string) })),

                // End exception block, continue thereafter (Do you want an immediate return?)
                new CodeInstruction(OpCodes.Nop).WithBlocks(exceptionEnd),

                new CodeInstruction(OpCodes.Nop).WithLabels(normalProcessing),

            });


            for (int z = 0; z < newInstructions.Count; z++)
            {
                yield return newInstructions[z];
            }

            ListPool<CodeInstruction>.Shared.Return(newInstructions);
        }
    }
}
