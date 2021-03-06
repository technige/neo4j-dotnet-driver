﻿// Copyright (c) 2002-2018 "Neo Technology,"
// Network Engine for Objects in Lund AB [http://neotechnology.com]
// 
// This file is part of Neo4j.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;

namespace Neo4j.Driver.V1
{
    /// <summary>
    /// Represents temporal amount containing months, days, seconds and nanoseconds. 
    /// <remarks>A duration can hold a negative value.</remarks>
    /// </summary>
    public struct CypherDuration : ICypherValue, IEquatable<CypherDuration>
    {

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDuration" /> in terms of <see cref="Seconds"/>
        /// </summary>
        /// <param name="seconds"><see cref="Seconds"/></param>
        public CypherDuration(long seconds)
            : this(seconds, 0)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDuration" /> in terms of <see cref="Seconds"/> 
        /// and <see cref="Nanos"/>
        /// </summary>
        /// <param name="seconds"><see cref="Seconds"/></param>
        /// <param name="nanos"><see cref="Nanos"/></param>
        public CypherDuration(long seconds, int nanos)
            : this(0, seconds, nanos)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDuration" /> in terms of <see cref="Days"/>, 
        /// <see cref="Seconds"/> and <see cref="Nanos"/>
        /// </summary>
        /// <param name="days"><see cref="Days"/></param>
        /// <param name="seconds"><see cref="Seconds"/></param>
        /// <param name="nanos"><see cref="Nanos"/></param>
        public CypherDuration(long days, long seconds, int nanos)
            : this(0, days, seconds, nanos)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="CypherDuration" /> with all supported temporal fields
        /// </summary>
        /// <param name="months"><see cref="Months"/></param>
        /// <param name="days"><see cref="Days"/></param>
        /// <param name="seconds"><see cref="Seconds"/></param>
        /// <param name="nanos"><see cref="Nanos"/></param>
        public CypherDuration(long months, long days, long seconds, int nanos)
        {
            Months = months;
            Days = days;
            Seconds = seconds;
            Nanos = nanos;
        }

        /// <summary>
        /// The number of months in this duration.
        /// </summary>
        public long Months { get; }

        /// <summary>
        /// The number of days in this duration.
        /// </summary>
        public long Days { get; }

        /// <summary>
        /// The number of seconds in this duration.
        /// </summary>
        public long Seconds { get; }

        /// <summary>
        /// The number of nanoseconds in this duration.
        /// </summary>
        public int Nanos { get; }

        /// <summary>
        /// Returns a value indicating whether the value of this instance is equal to the 
        /// value of the specified <see cref="CypherDuration"/> instance. 
        /// </summary>
        /// <param name="other">The object to compare to this instance.</param>
        /// <returns><code>true</code> if the <code>value</code> parameter equals the value of 
        /// this instance; otherwise, <code>false</code></returns>
        public bool Equals(CypherDuration other)
        {
            return Months == other.Months && Days == other.Days && Seconds == other.Seconds && Nanos == other.Nanos;
        }

        /// <summary>
        /// Returns a value indicating whether this instance is equal to a specified object.
        /// </summary>
        /// <param name="obj">The object to compare to this instance.</param>
        /// <returns><code>true</code> if <code>value</code> is an instance of <see cref="CypherDuration"/> and 
        /// equals the value of this instance; otherwise, <code>false</code></returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is CypherDuration && Equals((CypherDuration) obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Months.GetHashCode();
                hashCode = (hashCode * 397) ^ Days.GetHashCode();
                hashCode = (hashCode * 397) ^ Seconds.GetHashCode();
                hashCode = (hashCode * 397) ^ Nanos;
                return hashCode;
            }
        }

        /// <summary>
        /// Converts the value of the current <see cref="CypherDuration"/> object to its equivalent string representation.
        /// </summary>
        /// <returns>String representation of this Point.</returns>
        public override string ToString()
        {
            return $"Duration{{months: {Months}, days: {Days}, seconds: {Seconds}, nanos: {Nanos}}}";
        }
    }
}