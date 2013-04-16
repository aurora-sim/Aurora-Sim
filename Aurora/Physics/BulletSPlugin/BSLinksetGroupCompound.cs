/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyrightD
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Collections.Generic;
using System.Text;

using OMV = OpenMetaverse;

namespace OpenSim.Region.Physics.BulletSPlugin
{
public sealed class BSLinksetGroupCompound : BSLinkset
{
    private static string LogHeader = "[BULLETSIM LINKSET COMPOUND]";

    public BSLinksetGroupCompound(BSScene scene, BSPrimLinkable parent)
        : base(scene, parent)
    {
    }

    // For compound implimented linksets, if there are children, use compound shape for the root.
    public override BSPhysicsShapeType PreferredPhysicalShape(BSPrimLinkable requestor)
    { 
        // Returning 'unknown' means we don't have a preference.
        BSPhysicsShapeType ret = BSPhysicsShapeType.SHAPE_UNKNOWN;
        if (IsRoot(requestor) && HasAnyChildren && requestor.IsPhysical)
        {
            ret = BSPhysicsShapeType.SHAPE_COMPOUND;
        }
        // DetailLog("{0},BSLinksetCompound.PreferredPhysicalShape,call,shape={1}", LinksetRoot.LocalID, ret);
        return ret;
    }

    // When physical properties are changed the linkset needs to recalculate
    //   its internal properties.
    public override void Refresh(BSPrimLinkable requestor)
    {
        base.Refresh(requestor);

        // Something changed so do the rebuilding thing
        // ScheduleRebuild();
    }

    // Schedule a refresh to happen after all the other taint processing.
    protected override void ScheduleRebuild(BSPrimLinkable requestor)
    {
        DetailLog("{0},BSLinksetCompound.ScheduleRebuild,,rebuilding={1},hasChildren={2},actuallyScheduling={3}",
                            requestor.LocalID, Rebuilding, HasAnyChildren, (!Rebuilding && HasAnyChildren));
        // When rebuilding, it is possible to set properties that would normally require a rebuild.
        //    If already rebuilding, don't request another rebuild.
        //    If a linkset with just a root prim (simple non-linked prim) don't bother rebuilding.
        if (!Rebuilding && HasAnyChildren)
        {
            PhysicsScene.PostTaintObject("BSLinksetCompound.ScheduleRebuild", LinksetRoot.LocalID, delegate()
            {
                if (HasAnyChildren)
                    RecomputeLinksetCompound();
            });
        }
    }

    // The object is going dynamic (physical). Do any setup necessary for a dynamic linkset.
    // Only the state of the passed object can be modified. The rest of the linkset
    //     has not yet been fully constructed.
    // Return 'true' if any properties updated on the passed object.
    // Called at taint-time!
    public override bool MakeDynamic(BSPrimLinkable child)
    {
        bool ret = false;
        DetailLog("{0},BSLinksetCompound.MakeDynamic,call,IsRoot={1}", child.LocalID, IsRoot(child));
        if (IsRoot(child))
        {
            // The root is going dynamic. Rebuild the linkset so parts and mass get computed properly.
            ScheduleRebuild(LinksetRoot);
        }
        /*else
        {
            // The origional prims are removed from the world as the shape of the root compound
            //     shape takes over.
            PhysicsScene.PE.AddToCollisionFlags(child.PhysBody, CollisionFlags.CF_NO_CONTACT_RESPONSE);
            PhysicsScene.PE.ForceActivationState(child.PhysBody, ActivationState.DISABLE_SIMULATION);
            // We don't want collisions from the old linkset children.
            PhysicsScene.PE.RemoveFromCollisionFlags(child.PhysBody, CollisionFlags.BS_SUBSCRIBE_COLLISION_EVENTS);

            child.PhysBody.collisionType = CollisionType.LinksetChild;

            ret = true;
        }*/
        return ret;
    }

    // The object is going static (non-physical). Do any setup necessary for a static linkset.
    // Return 'true' if any properties updated on the passed object.
    // This doesn't normally happen -- OpenSim removes the objects from the physical
    //     world if it is a static linkset.
    // Called at taint-time!
    public override bool MakeStatic(BSPrimLinkable child)
    {
        if (!LinksetRoot.IsPhysical) return false;
        bool ret = false;
        DetailLog("{0},BSLinksetCompound.MakeStatic,call,IsRoot={1}", child.LocalID, IsRoot(child));
        if (IsRoot(child))
        {
            ScheduleRebuild(LinksetRoot);
        }
        /*else
        {
            // The non-physical children can come back to life.
            PhysicsScene.PE.RemoveFromCollisionFlags(child.PhysBody, CollisionFlags.CF_NO_CONTACT_RESPONSE);

            child.PhysBody.collisionType = CollisionType.LinksetChild;

            // Don't force activation so setting of DISABLE_SIMULATION can stay if used.
            PhysicsScene.PE.Activate(child.PhysBody, false);
            ret = true;
        }*/
        return ret;
    }

    // 'physicalUpdate' is true if these changes came directly from the physics engine. Don't need to rebuild then.
    // Called at taint-time.
    public override void UpdateProperties(UpdatedProperties whichUpdated, BSPrimLinkable updated)
    {
        if (!LinksetRoot.IsPhysical) return;
        // The user moving a child around requires the rebuilding of the linkset compound shape
        // One problem is this happens when a border is crossed -- the simulator implementation
        //    stores the position into the group which causes the move of the object
        //    but it also means all the child positions get updated.
        //    What would cause an unnecessary rebuild so we make sure the linkset is in a
        //    region before bothering to do a rebuild.
        if (!IsRoot(updated) && PhysicsScene.TerrainManager.IsWithinKnownTerrain(LinksetRoot.RawPosition))
        {
            // If a child of the linkset is updating only the position or rotation, that can be done
            //    without rebuilding the linkset.
            // If a handle for the child can be fetch, we update the child here. If a rebuild was
            //    scheduled by someone else, the rebuild will just replace this setting.

            bool updatedChild = false;
            // Anything other than updating position or orientation usually means a physical update
            //     and that is caused by us updating the object.
            if ((whichUpdated & ~(UpdatedProperties.Position | UpdatedProperties.Orientation)) == 0)
            {
                    // Find the physical instance of the child 
                if (LinksetRoot.PhysShape.HasPhysicalShape && PhysicsScene.PE.IsCompound(LinksetRoot.PhysShape))
                {
                    // It is possible that the linkset is still under construction and the child is not yet
                    //    inserted into the compound shape. A rebuild of the linkset in a pre-step action will
                    //    build the whole thing with the new position or rotation.
                    // The index must be checked because Bullet references the child array but does no validity
                    //    checking of the child index passed.
                    int numLinksetChildren = PhysicsScene.PE.GetNumberOfCompoundChildren(LinksetRoot.PhysShape);
                    if (updated.LinksetChildIndex < numLinksetChildren)
                    {
                        BulletShape linksetChildShape = PhysicsScene.PE.GetChildShapeFromCompoundShapeIndex(LinksetRoot.PhysShape, updated.LinksetChildIndex);
                        if (linksetChildShape.HasPhysicalShape)
                        {
                            // Found the child shape within the compound shape
                            PhysicsScene.PE.UpdateChildTransform(LinksetRoot.PhysShape, updated.LinksetChildIndex,
                                                                        updated.RawPosition - LinksetRoot.RawPosition,
                                                                        updated.RawOrientation * OMV.Quaternion.Inverse(LinksetRoot.RawOrientation),
                                                                        true /* shouldRecalculateLocalAabb */);
                            updatedChild = true;
                            DetailLog("{0},BSLinksetCompound.UpdateProperties,changeChildPosRot,whichUpdated={1},pos={2},rot={3}",
                                                        updated.LocalID, whichUpdated, updated.RawPosition, updated.RawOrientation);
                        }
                        else    // DEBUG DEBUG
                        {       // DEBUG DEBUG
                            DetailLog("{0},BSLinksetCompound.UpdateProperties,couldNotUpdateChild,noChildShape,shape={1}",
                                                                        updated.LocalID, linksetChildShape);
                        }       // DEBUG DEBUG
                    }
                    else    // DEBUG DEBUG
                    {       // DEBUG DEBUG
                        // the child is not yet in the compound shape. This is non-fatal.
                        DetailLog("{0},BSLinksetCompound.UpdateProperties,couldNotUpdateChild,childNotInCompoundShape,numChildren={1},index={2}",
                                                                    updated.LocalID, numLinksetChildren, updated.LinksetChildIndex);
                    }       // DEBUG DEBUG
                }
                else    // DEBUG DEBUG
                {       // DEBUG DEBUG
                    DetailLog("{0},BSLinksetCompound.UpdateProperties,couldNotUpdateChild,noBodyOrNotCompound", updated.LocalID);
                }       // DEBUG DEBUG

                if (!updatedChild)
                {
                    // If couldn't do the individual child, the linkset needs a rebuild to incorporate the new child info.
                    // Note: there are several ways through this code that will not update the child if
                    //    the linkset is being rebuilt. In this case, scheduling a rebuild is a NOOP since
                    //    there will already be a rebuild scheduled.
                    DetailLog("{0},BSLinksetCompound.UpdateProperties,couldNotUpdateChild.schedulingRebuild,whichUpdated={1}",
                                                                    updated.LocalID, whichUpdated);
                    updated.LinksetInfo = null; // setting to 'null' causes relative position to be recomputed.
                    ScheduleRebuild(updated);
                }
            }
        }
    }

    // Routine called when rebuilding the body of some member of the linkset.
    // Since we don't keep in world relationships, do nothing unless it's a child changing.
    // Returns 'true' of something was actually removed and would need restoring
    // Called at taint-time!!
    public override bool RemoveBodyDependencies(BSPrimLinkable child)
    {
        bool ret = false;

        DetailLog("{0},BSLinksetCompound.RemoveBodyDependencies,refreshIfChild,rID={1},rBody={2},isRoot={3}",
                        child.LocalID, LinksetRoot.LocalID, LinksetRoot.PhysBody, IsRoot(child));

        if (!IsRoot(child))
        {
            // Because it is a convenient time, recompute child world position and rotation based on
            //    its position in the linkset.
            RecomputeChildWorldPosition(child, true /* inTaintTime */);
            child.LinksetInfo = null;
        }

        // Cannot schedule a refresh/rebuild here because this routine is called when
        //     the linkset is being rebuilt.
        // InternalRefresh(LinksetRoot);

        return ret;
    }

    // When the linkset is built, the child shape is added to the compound shape relative to the
    //    root shape. The linkset then moves around but this does not move the actual child
    //    prim. The child prim's location must be recomputed based on the location of the root shape.
    private void RecomputeChildWorldPosition(BSPrimLinkable child, bool inTaintTime)
    {
        // For the moment (20130201), disable this computation (converting the child physical addr back to
        //    a region address) until we have a good handle on center-of-mass offsets and what the physics
        //    engine moving a child actually means.
        // The simulator keeps track of where children should be as the linkset moves. Setting
        //    the pos/rot here does not effect that knowledge as there is no good way for the
        //    physics engine to send the simulator an update for a child.

        /*
        BSLinksetCompoundInfo lci = child.LinksetInfo as BSLinksetCompoundInfo;
        if (lci != null)
        {
            if (inTaintTime)
            {
                OMV.Vector3 oldPos = child.RawPosition;
                child.ForcePosition = LinksetRoot.RawPosition + lci.OffsetFromRoot;
                child.ForceOrientation = LinksetRoot.RawOrientation * lci.OffsetRot;
                DetailLog("{0},BSLinksetCompound.RecomputeChildWorldPosition,oldPos={1},lci={2},newPos={3}",
                                            child.LocalID, oldPos, lci, child.RawPosition);
            }
            else
            {
                // TaintedObject is not used here so the raw position is set now and not at taint-time.
                child.Position = LinksetRoot.RawPosition + lci.OffsetFromRoot;
                child.Orientation = LinksetRoot.RawOrientation * lci.OffsetRot;
            }
        }
        else
        {
            // This happens when children have been added to the linkset but the linkset
            //     has not been constructed yet. So like, at taint time, adding children to a linkset
            //     and then changing properties of the children (makePhysical, for instance)
            //     but the post-print action of actually rebuilding the linkset has not yet happened.
            // PhysicsScene.Logger.WarnFormat("{0} Restoring linkset child position failed because of no relative position computed. ID={1}",
            //                                 LogHeader, child.LocalID);
            DetailLog("{0},BSLinksetCompound.recomputeChildWorldPosition,noRelativePositonInfo", child.LocalID);
        }
        */
    }

    // ================================================================

    // Add a new child to the linkset.
    // Called while LinkActivity is locked.
    protected override void AddChildToLinkset(BSPrimLinkable child, bool scheduleRebuild)
    {
        if (!HasChild(child))
        {
            m_children.Add(child);

            DetailLog("{0},BSLinksetCompound.AddChildToLinkset,call,child={1}", LinksetRoot.LocalID, child.LocalID);

            // Rebuild the compound shape with the new child shape included
            if (scheduleRebuild)
                ScheduleRebuild(child);
        }
        return;
    }

    // Remove the specified child from the linkset.
    // Safe to call even if the child is not really in the linkset.
    protected override void RemoveChildFromLinkset(BSPrimLinkable child)
    {
        child.ClearDisplacement();

        if (m_children.Remove(child))
        {
            DetailLog("{0},BSLinksetCompound.RemoveChildFromLinkset,call,rID={1},rBody={2},cID={3},cBody={4}",
                            child.LocalID,
                            LinksetRoot.LocalID, LinksetRoot.PhysBody.AddrString,
                            child.LocalID, child.PhysBody.AddrString);

            // Cause the child's body to be rebuilt and thus restored to normal operation
            RecomputeChildWorldPosition(child, false);
            child.LinksetInfo = null;
            child.ForceBodyShapeRebuild(false);

            if (!HasAnyChildren)
            {
                // The linkset is now empty. The root needs rebuilding.
                LinksetRoot.ForceBodyShapeRebuild(false);
            }
            else
            {
                // Rebuild the compound shape with the child removed
                ScheduleRebuild(LinksetRoot);
            }
        }
        return;
    }

    // Called before the simulation step to make sure the compound based linkset
    //    is all initialized.
    // Constraint linksets are rebuilt every time.
    // Note that this works for rebuilding just the root after a linkset is taken apart.
    // Called at taint time!!
    private bool disableCOM = false;
    private void RecomputeLinksetCompound()
    {
        try
        {
            // Suppress rebuilding while rebuilding. (We know rebuilding is on only one thread.)
            Rebuilding = true;

            if (LinksetRoot.IsPhysical)
            {
                // Cause the root shape to be rebuilt as a compound object with just the root in it
                LinksetRoot.ForceBodyShapeRebuild(true /* inTaintTime */);

                // The center of mass for the linkset is the geometric center of the group.
                // Compute a displacement for each component so it is relative to the center-of-mass.
                // Bullet presumes an object's origin (relative <0,0,0>) is its center-of-mass
                OMV.Vector3 centerOfMassW = LinksetRoot.RawPosition;
                if (!disableCOM)
                {
                    // Compute a center-of-mass in world coordinates.
                    centerOfMassW = ComputeLinksetCenterOfMass();
                }

                OMV.Quaternion invRootOrientation = OMV.Quaternion.Inverse(LinksetRoot.RawOrientation);

                // 'centerDisplacement' is the value to subtract from children to give physical offset position
                OMV.Vector3 centerDisplacement = (centerOfMassW - LinksetRoot.RawPosition) * invRootOrientation;
                //Set the COM in bullet
                PhysicsScene.PE.SetCenterOfMassByPosRot(LinksetRoot.PhysBody, centerDisplacement, OMV.Quaternion.Identity);

                // This causes the physical position of the root prim to be offset to accomodate for the displacements
                LinksetRoot.ForcePosition = LinksetRoot.RawPosition;

                // Update the local transform for the root child shape so it is offset from the <0,0,0> which is COM
                //Not necessary, as the COM is set in bullet now, and this was always Vector3.Zero beforehand, which is of no use
                //PhysicsScene.PE.UpdateChildTransform(LinksetRoot.PhysShape, 0 /* childIndex */,
                //                                    -centerDisplacement,
                //                                    OMV.Quaternion.Identity, // LinksetRoot.RawOrientation,
                //                                    false /* shouldRecalculateLocalAabb (is done later after linkset built) */);
                LinksetRoot.LinksetChildIndex = 0;

                DetailLog("{0},BSLinksetCompound.RecomputeLinksetCompound,COM,com={1},rootPos={2},centerDisp={3}",
                                        LinksetRoot.LocalID, centerOfMassW, LinksetRoot.RawPosition, centerDisplacement);

                DetailLog("{0},BSLinksetCompound.RecomputeLinksetCompound,start,rBody={1},rShape={2},numChildren={3}",
                                LinksetRoot.LocalID, LinksetRoot.PhysBody, LinksetRoot.PhysShape, NumberOfChildren);

                // Add a shape for each of the other children in the linkset
                int memberIndex = 1;
                lock (m_linksetActivityLock)
                {
                    foreach (BSPrimLinkable cPrim in m_children)
                    {
                        cPrim.LinksetChildIndex = memberIndex;

                        if (cPrim.PhysBody.AddrString == "unknown")
                        {
                            if (!PhysicsScene.Shapes.GetBodyAndShape(true, PhysicsScene.World, cPrim))
                            {
                            }
                        }
                        if (cPrim.PhysShape.isNativeShape)
                        {
                            // A native shape is turned into a hull collision shape because native
                            //    shapes are not shared so we have to hullify it so it will be tracked
                            //    and freed at the correct time. This also solves the scaling problem
                            //    (native shapes scale but hull/meshes are assumed to not be).
                            // TODO: decide of the native shape can just be used in the compound shape.
                            //    Use call to CreateGeomNonSpecial().
                            BulletShape saveShape = cPrim.PhysShape;
                            cPrim.PhysShape.Clear();        // Don't let the create free the child's shape
                            PhysicsScene.Shapes.CreateGeomMeshOrHull(cPrim, null);
                            BulletShape newShape = cPrim.PhysShape;
                            cPrim.PhysShape = saveShape;

                            OMV.Vector3 offsetPos = (cPrim.RawPosition - LinksetRoot.RawPosition) * invRootOrientation;
                            OMV.Quaternion offsetRot = cPrim.RawOrientation * invRootOrientation;
                            PhysicsScene.PE.AddChildShapeToCompoundShape(LinksetRoot.PhysShape, newShape, offsetPos, offsetRot);
                            DetailLog("{0},BSLinksetCompound.RecomputeLinksetCompound,addNative,indx={1},rShape={2},cShape={3},offPos={4},offRot={5}",
                                        LinksetRoot.LocalID, memberIndex, LinksetRoot.PhysShape, newShape, offsetPos, offsetRot);
                        }
                        else
                        {
                            // For the shared shapes (meshes and hulls), just use the shape in the child.
                            // The reference count added here will be decremented when the compound shape
                            //     is destroyed in BSShapeCollection (the child shapes are looped over and dereferenced).
                            if (PhysicsScene.Shapes.ReferenceShape(cPrim.PhysShape))
                            {
                                PhysicsScene.Logger.ErrorFormat("{0} Rebuilt sharable shape when building linkset! Region={1}, primID={2}, shape={3}",
                                                    LogHeader, PhysicsScene.RegionName, cPrim.LocalID, cPrim.PhysShape);
                            }
                            OMV.Vector3 offsetPos = (cPrim.RawPosition - LinksetRoot.RawPosition) * invRootOrientation;
                            OMV.Quaternion offsetRot = cPrim.RawOrientation * invRootOrientation;
                            PhysicsScene.PE.AddChildShapeToCompoundShape(LinksetRoot.PhysShape, cPrim.PhysShape, offsetPos, offsetRot);
                            DetailLog("{0},BSLinksetCompound.RecomputeLinksetCompound,addNonNative,indx={1},rShape={2},cShape={3},offPos={4},offRot={5}",
                                        LinksetRoot.LocalID, memberIndex, LinksetRoot.PhysShape, cPrim.PhysShape, offsetPos, offsetRot);

                        }

                        PhysicsScene.PE.AddToCollisionFlags(cPrim.PhysBody, CollisionFlags.CF_NO_CONTACT_RESPONSE);
                        PhysicsScene.PE.ForceActivationState(cPrim.PhysBody, ActivationState.DISABLE_SIMULATION);
                        // We don't want collisions from the old linkset children.
                        PhysicsScene.PE.RemoveFromCollisionFlags(cPrim.PhysBody, CollisionFlags.BS_SUBSCRIBE_COLLISION_EVENTS);

                        cPrim.PhysBody.collisionType = CollisionType.LinksetChild;
                        memberIndex++;
                    }
                }

                // With all of the linkset packed into the root prim, it has the mass of everyone.
                LinksetMass = ComputeLinksetMass();
                LinksetRoot.UpdatePhysicalMassProperties(LinksetMass, true);

                // Enable the physical position updator to return the position and rotation of the root shape
                PhysicsScene.PE.AddToCollisionFlags(LinksetRoot.PhysBody, CollisionFlags.BS_RETURN_ROOT_COMPOUND_SHAPE);

                // See that the Aabb surrounds the new shape
                PhysicsScene.PE.RecalculateCompoundShapeLocalAabb(LinksetRoot.PhysShape);
            }
            else
            {
                // Cause the root shape to be rebuilt as a compound object with just the root in it
                LinksetRoot.ForceBodyShapeRebuild(true /* inTaintTime */);

                //Update the AABB so that things will collide properly
                PhysicsScene.PE.UpdateSingleAabb(PhysicsScene.World, LinksetRoot.PhysBody);

                OMV.Quaternion invRootOrientation = OMV.Quaternion.Inverse(LinksetRoot.RawOrientation);
                
                // This causes the physical position of the root prim to be offset to accomodate for the displacements
                LinksetRoot.ForcePosition = LinksetRoot.RawPosition;
                int memberIndex = 0;
                LinksetRoot.LinksetChildIndex = memberIndex++;
                lock (m_linksetActivityLock)
                {
                    foreach (BSPrimLinkable cPrim in m_children)
                    {
                        OMV.Vector3 offsetPos = (cPrim.RawPosition - LinksetRoot.RawPosition) * invRootOrientation;
                        OMV.Quaternion offsetRot = cPrim.RawOrientation * invRootOrientation;

                        // Build a simple shape for this child prim (given that we aren't allowing it to be dynamic
                        cPrim.ForceBodyShapeRebuild(true);

                        // This causes the physical position of the root prim to be offset to accomodate for the displacements
                        cPrim.ForcePosition = cPrim.RawPosition;
                        cPrim.LinksetChildIndex = memberIndex++;

                        //Update AABB for collisions
                        PhysicsScene.PE.UpdateSingleAabb(PhysicsScene.World, cPrim.PhysBody);
                    }
                }
            }
        }
        finally
        {
            Rebuilding = false;
        }
    }
}
}