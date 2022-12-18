using System;
using DCAF._lib;
using TetraPak.XP;
using Xunit;

namespace UnitTests
{
    public class TimeFrameTests
    {
        [Fact]
        public void Test_no_overlap()
        {   // a:  <---->
            // b:        <---->
            // ==  <---->
            var a = new TimeFrame(new DateTime(2000, 1, 1), new DateTime(2000, 1, 5));
            var b = new TimeFrame(new DateTime(2000, 1, 5), new DateTime(2000, 1, 10));
            var c = new TimeFrame(new DateTime(2000, 1, 10), new DateTime(2000, 1, 15));
            var diffs = a.Subtract(b);
            Assert.Single(diffs);
            diffs = c.Subtract(b);
            Assert.Single(diffs);
        }

        [Fact]
        public void Test_subtract_partly_overlapping()
        {
            var a = new TimeFrame(new DateTime(2000, 1, 1), new DateTime(2000, 1, 10));
            var b = new TimeFrame(new DateTime(2000, 1, 5), new DateTime(2000, 1, 15));
            
            // start
            // a:  <--------->
            // b:      <--------->
            // ==  <--->
            var diffs = a.Subtract(b);
            Assert.Single(diffs);
            var diff = diffs[0];
            Assert.Equal(new DateTime(2000, 1, 1), diff.From);
            Assert.Equal(new DateTime(2000, 1, 5), diff.To);
            
            // end
            // b:       <--------->
            // a:  <--------->
            // ==             <--->
            diffs = b.Subtract(a);
            Assert.Single(diffs);
            diff = diffs[0];
            Assert.Equal(new DateTime(2000, 1, 10), diff.From);
            Assert.Equal(new DateTime(2000, 1, 15), diff.To);
        }
        
        [Fact]
        public void Test_subtract_fully_overlapping()
        {
            // a:  <------------------>
            // b:       <-------->
            // ==  <--->          <--->
            var a = new TimeFrame(new DateTime(2000, 1, 1), new DateTime(2000, 1, 20));
            var b = new TimeFrame(new DateTime(2000, 1, 5), new DateTime(2000, 1, 15));
            var diff = a.Subtract(b);
            Assert.Equal(2, diff.Length);
            var left = diff[0];
            Assert.Equal(new DateTime(2000, 1, 1), left.From);
            Assert.Equal(new DateTime(2000, 1, 5), left.To);
            var right = diff[1];
            Assert.Equal(new DateTime(2000, 1, 15), right.From);
            Assert.Equal(new DateTime(2000, 1, 20), right.To);
            
            // b:       <-------->
            // a:  <------------------>
            // ==  
            diff = b.Subtract(a);
            Assert.Empty(diff);
        }

        [Fact]
        public void Test_subtracting_multiple_smaller_overlapping_timeframes()
        {
            // a:    <------------------------> 
            // b+c:        <--->     <---->
            // ==    <--->      <--->      <--->
            var a = new TimeFrame(new DateTime(2000, 1, 1), new DateTime(2000, 1, 30));
            var b = new TimeFrame(new DateTime(2000, 1, 5), new DateTime(2000, 1, 10));
            var c = new TimeFrame(new DateTime(2000, 1, 15), new DateTime(2000, 1, 20)); 
            var diff = a.Subtract(b, c);
            Assert.Equal(3, diff.Length);
            Assert.Equal(new TimeFrame(new DateTime(2000, 1, 1), new DateTime(2000, 1, 5)), diff[0]);
            Assert.Equal(new TimeFrame(new DateTime(2000, 1, 10), new DateTime(2000, 1, 15)), diff[1]);
            Assert.Equal(new TimeFrame(new DateTime(2000, 1, 20), new DateTime(2000, 1, 30)), diff[2]);
            
            // a:    <------------------> 
            // b+c:        <--->     <---->
            // ==    <--->      <--->
            a = new TimeFrame(new DateTime(2000, 1, 1), new DateTime(2000, 1, 15));
            diff = a.Subtract(b, c);
            Assert.Equal(2, diff.Length);
            Assert.Equal(new TimeFrame(new DateTime(2000, 1, 1), new DateTime(2000, 1, 5)), diff[0]);
            Assert.Equal(new TimeFrame(new DateTime(2000, 1, 10), new DateTime(2000, 1, 15)), diff[1]);
            
            
            // a:            <-----------------> 
            // b+c:        <--->     <---->
            // ==               <--->      <--->
            a = new TimeFrame(new DateTime(2000, 1, 7), new DateTime(2000, 1, 25));
            diff = a.Subtract(b, c);
            Assert.Equal(new TimeFrame(new DateTime(2000, 1, 10), new DateTime(2000, 1, 15)), diff[0]);
            Assert.Equal(new TimeFrame(new DateTime(2000, 1, 20), new DateTime(2000, 1, 30)), diff[1]);
            
            // a:        <---> 
            // b+c:  <------------->    <-------->
            // ==              
            a = new TimeFrame(new DateTime(2000, 1, 5), new DateTime(2000, 1, 10));
            b = new TimeFrame(new DateTime(2000, 1, 1), new DateTime(2000, 1, 15));
            c = new TimeFrame(new DateTime(2000, 1, 20), new DateTime(2000, 1, 30));
            diff = a.Subtract(b, c);
            Assert.Empty(diff);
        }

    }
}