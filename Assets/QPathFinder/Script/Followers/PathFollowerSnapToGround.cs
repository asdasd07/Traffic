using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

    
    public class PathFollowerSnapToGround : PathFollower
    {
        Vector3 directionOfRayCast;
        float offsetDistanceFromPoint;
        int maxDistanceForRayCast;
        LayerMask backgroundLayerMask;
        float offsetDistanceToFloatFromGround;

        public void Init ( Vector3 directionOfRayCast, float offsetDistanceFromPoint, float offsetDistanceToFloatFromGround, int maxDistanceForRayCast, int groundLayer )
        {
            this.directionOfRayCast = directionOfRayCast.normalized;
            this.offsetDistanceFromPoint = offsetDistanceFromPoint;
            this.maxDistanceForRayCast = maxDistanceForRayCast;
            this.offsetDistanceToFloatFromGround = offsetDistanceToFloatFromGround;

            backgroundLayerMask = 1 << groundLayer;
        }

        public override Vector3 ConvertPointIfNeeded ( Vector3 point )
        {
            RaycastHit hitInfo;
            if (Physics.Raycast ( point + offsetDistanceFromPoint * (-directionOfRayCast), directionOfRayCast,out hitInfo, maxDistanceForRayCast, backgroundLayerMask.value ) )
            {
                Vector3 hitPos = hitInfo.point;
				hitPos = hitPos + offsetDistanceToFloatFromGround * (-directionOfRayCast);
				return hitPos;
            }

            if ( Logger.CanLogError )
            {

                Logger.LogError("Ground not found at " + point + ". Could not snap to ground properly! Raycast origin: " + (point + offsetDistanceFromPoint * (-directionOfRayCast)) +
                        " raycast direction:" + directionOfRayCast + " Distance of raycase:" + maxDistanceForRayCast);


                Debug.DrawLine ( point + offsetDistanceFromPoint * (-directionOfRayCast), point + offsetDistanceFromPoint * (-directionOfRayCast) + directionOfRayCast * maxDistanceForRayCast, Color.red, Logger.DrawLineDuration );
            }
            return point;
            
        }
		public override void MoveTo(int pointIndex)
		{
			var targetPos = pointsToFollow[pointIndex] ;

			var deltaPos = targetPos - transform.position;
        //deltaPos.z = 0f;
        transform.up = Vector3.up;
        transform.forward = deltaPos.normalized;

			if ( directionOfRayCast.x != 0 )
				targetPos.x = transform.position.x;
			else if ( directionOfRayCast.y != 0 )
				targetPos.y = transform.position.y;
			else if ( directionOfRayCast.z != 0 )
				targetPos.z = transform.position.z;

			var newTransformPos =	Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.smoothDeltaTime);
			newTransformPos = ConvertPointIfNeeded ( newTransformPos );;

			if ( Logger.CanLogInfo ) Debug.DrawLine( transform.position, newTransformPos, Color.blue, Logger.DrawLineDuration );

        transform.position = newTransformPos;
		}


		protected override bool IsOnPoint(int pointIndex) 
		{ 
			Vector3 finalPoint = ConvertPointIfNeeded(pointsToFollow[pointIndex]);
			float mag = (transform.position - finalPoint).sqrMagnitude; 
			return mag < 0.1f;
		}

    }
