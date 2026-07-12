using FFS.Libraries.StaticEcs;
using Fixed;
using Fixed32;
using Game.Core;
using Shenanicode.Rollback;

namespace Game {
	public abstract partial class Core<TWorld> where TWorld : struct, ISessionType, IWorldType {
		public class CollisionResolutionSystem : ISystem {
			public void Update() {
				W.Query().BatchDelete<W.Multi<ContactPair>>();

				var broadPhase = W.GetResource<BroadPhase>();
				broadPhase.CollectPairs();

				var pairs = broadPhase.Pairs;
				for (var i = 0; i < pairs.Count; i++) {
					var (entityA, entityB) = pairs[i];

					ref readonly var colliderA = ref entityA.Read<Collider>()!;
					ref readonly var colliderB = ref entityB.Read<Collider>()!;

					var delta = colliderB.WorldPosition - colliderA.WorldPosition;
					var distSqr = Fixed64.FVector2.LengthSqr(delta.To64());
					var radiusSum = colliderA.Radius + colliderB.Radius;

					if (distSqr < (radiusSum * radiusSum).To64()) {
						// CollectPairs yields each pair once, so no contact-dedup check is needed.
						Resolve(entityA, entityB, colliderA, colliderB, delta, distSqr.To32());
						AddContact(entityA, entityB);
					}
				}
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
				var invIA = FP.One / (colA.Radius * colA.Radius / 2);
				var invIB = FP.One / (colB.Radius * colB.Radius / 2);

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
