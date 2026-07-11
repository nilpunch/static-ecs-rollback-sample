using FFS.Libraries.StaticEcs;
using Fixed32;
using Game.Core;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class CollisionResolutionSystem : ISystem {
			public void Update() {
				W.Query().BatchDelete<W.Multi<ContactPair>>();

				var broadPhase = W.GetResource<BroadPhase>();

				W.Query().For(ref broadPhase,
					static (ref BroadPhase bp, W.Entity entityA, in Collider colliderA) => {
						var nearby = bp.FindNearbyEntities(colliderA.WorldBounds);
						for (var i = 0; i < nearby.Count; i++) {
							var entityB = nearby[i];

							if (entityA == entityB || entityA.ID > entityB.ID) {
								continue;
							}

							if (HasContact(entityA, entityB)) {
								continue;
							}

							ref readonly var colliderB = ref entityB.Read<Collider>()!;

							var delta = entityB.Read<Collider>().WorldPosition - colliderA.WorldPosition;
							var distSqr = FVector2.LengthSqr(delta);
							var radiusSum = colliderA.Radius + colliderB.Radius;

							if (distSqr < radiusSum * radiusSum) {
								Resolve(entityA, entityB, colliderA, colliderB, delta, distSqr);
								AddContact(entityA, entityB);
							}
						}
					});
			}

			private static bool HasContact(W.Entity a, W.Entity b) {
				if (!a.Has<W.Multi<ContactPair>>()) {
					return false;
				}
				ref var pairs = ref a.Ref<W.Multi<ContactPair>>();
				for (var i = 0; i < pairs.Length; i++) {
					if (pairs[i].Other == b.GID) {
						return true;
					}
				}
				return false;
			}

			private static void AddContact(W.Entity a, W.Entity b) {
				a.Add<W.Multi<ContactPair>>().Add(new ContactPair(b.GID));
				b.Add<W.Multi<ContactPair>>().Add(new ContactPair(a.GID));
			}

			private static void Resolve(W.Entity entityA, W.Entity entityB, in Collider colA, in Collider colB, FVector2 delta, FP distSqr) {
				var dist = FP.Sqrt(distSqr);
				var normal = dist > FP.Epsilon ? delta / dist : new FVector2(FP.One, FP.Zero);
				var penetration = (colA.Radius + colB.Radius) - dist;

				ref var velA = ref entityA.Ref<Velocity>()!;
				ref var velB = ref entityB.Ref<Velocity>()!;
				ref var bodyA = ref entityA.Ref<PhysicalBody>()!;
				ref var bodyB = ref entityB.Ref<PhysicalBody>()!;

				var rA = colA.WorldPosition - bodyA.WorldCoM;
				var rB = colB.WorldPosition - bodyB.WorldCoM;

				var rAPerp = new FVector2(-rA.Y, rA.X);
				var rBPerp = new FVector2(-rB.Y, rB.X);

				var vA = velA.Linear + rAPerp * velA.Angular.Radians;
				var vB = velB.Linear + rBPerp * velB.Angular.Radians;

				var relativeVel = vB - vA;
				var velAlongNormal = FVector2.Dot(relativeVel, normal);

				if (velAlongNormal > 0) {
					// Positional correction only
					ApplyPositionalCorrection(ref bodyA, ref bodyB, normal, penetration);
					return;
				}

				var e = FP.FromRatio(500, 1000); // Restitution 0.5

				// Simple mass/inertia: mass=1, inertia=radius^2/2
				var invMassA = FP.One;
				var invMassB = FP.One;
				var invIA = FP.One / (colA.Radius * colA.Radius / 2.ToFP());
				var invIB = FP.One / (colB.Radius * colB.Radius / 2.ToFP());

				var rAPerpDotN = FVector2.Dot(rAPerp, normal);
				var rBPerpDotN = FVector2.Dot(rBPerp, normal);

				var denominator = invMassA + invMassB + (rAPerpDotN * rAPerpDotN) * invIA + (rBPerpDotN * rBPerpDotN) * invIB;

				var j = -(FP.One + e) * velAlongNormal / denominator;
				var impulse = normal * j;

				velA.Linear -= impulse * invMassA;
				velA.Angular = FAngle.FromRadians(velA.Angular.Radians - FVector2.Dot(rAPerp, impulse) * invIA);

				velB.Linear += impulse * invMassB;
				velB.Angular = FAngle.FromRadians(velB.Angular.Radians + FVector2.Dot(rBPerp, impulse) * invIB);

				ApplyPositionalCorrection(ref bodyA, ref bodyB, normal, penetration);
			}

			private static void ApplyPositionalCorrection(ref PhysicalBody bodyA, ref PhysicalBody bodyB, FVector2 normal, FP penetration) {
				var percent = FP.FromRatio(200, 1000); // 20%
				var slop = FP.FromRatio(10, 1000);     // 0.01
				var corrMag = (penetration - slop);
				if (corrMag < FP.Zero) {
					corrMag = FP.Zero;
				}
				var correction = normal * (corrMag / 2 * percent);
				bodyA.WorldCoM -= correction;
				bodyB.WorldCoM += correction;
			}
		}
	}
}
