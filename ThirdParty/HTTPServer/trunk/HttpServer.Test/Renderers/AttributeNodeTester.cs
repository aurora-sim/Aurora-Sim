using HttpServer.MVC.Rendering.Haml;
using HttpServer.MVC.Rendering.Haml.Nodes;
using Xunit;

namespace HttpServer.Test.Renderers
{
    
    public class AttributeNodeTester
    {

        [Fact]
        public void Test()
        {
            LineInfo line = new LineInfo(1, string.Empty);
            line.Set("%input{ type=\"checkbox\", value=testCase.Id, name=\"case\", class=lastCat.Replace(' ', '-')}", 0,
                     0);

            TagNode tagNode = new TagNode(null);
            NodeList nodes = new NodeList();
            AttributeNode node = new AttributeNode(tagNode);
            nodes.Add(node);
            nodes.Add(tagNode);

            int offset = 6;
            AttributeNode myNode  = (AttributeNode)node.Parse(nodes, tagNode, line, ref offset);
            bool t = false;
            string temp = myNode.ToCode(ref t, false);
            Assert.Equal("\"checkbox\"", myNode.GetAttribute("type").Value);
            Assert.Equal("testCase.Id", myNode.GetAttribute("value").Value);
            Assert.Equal("\"case\"", myNode.GetAttribute("name").Value);
            Assert.Equal("lastCat.Replace(' ', '-')", myNode.GetAttribute("class").Value);
        }

        public void TestPartial()
        {
            LineInfo line = new LineInfo(1, string.Empty);
            line.Set("%input{ value=\"this\"+testCase.Id+\"id\" }", 0,
                     0);

            TagNode tagNode = new TagNode(null);
            NodeList nodes = new NodeList();
            AttributeNode node = new AttributeNode(tagNode);
            nodes.Add(node);
            nodes.Add(tagNode);

            int offset = 6;
            AttributeNode myNode = (AttributeNode)node.Parse(nodes, tagNode, line, ref offset);

            bool inStr = true;
            string res = myNode.ToCode(ref inStr, false);

            Assert.Equal("\"this\"+testCase.Id+\"id\"", myNode.GetAttribute("value").Value);
            Assert.Equal("value=\"\"\"); sb.Append(\"this\"+testCase.Id+\"id\"); sb.Append(@\"\"\"", res);
        }

        public void TestPartial2()
        {
                        LineInfo line = new LineInfo(1, string.Empty);
            line.Set("%input{ value=\"http://\"+domain+\"/voicemail/listen/\" + item.Id }", 0, 0);

            TagNode tagNode = new TagNode(null);
            NodeList nodes = new NodeList();
            AttributeNode node = new AttributeNode(tagNode);
            nodes.Add(node);
            nodes.Add(tagNode);

            int offset = 6;
            AttributeNode myNode = (AttributeNode)node.Parse(nodes, tagNode, line, ref offset);

            bool inStr = true;
            string res = myNode.ToCode(ref inStr, false);

            Assert.Equal("\"this\"+testCase.Id+\"id\"", myNode.GetAttribute("value").Value);
            Assert.Equal("value=\"\"\"); sb.Append(\"this\"+testCase.Id+\"id\"); sb.Append(@\"\"\"", res);
        }
        

        public void TestPartial3()
        {
                        LineInfo line = new LineInfo(1, string.Empty);
            line.Set("%a{href=\"/settings/divert/remove/\", title=Language[\"RemoveDivert\"] }", 0, 0);

            TagNode tagNode = new TagNode(null);
            NodeList nodes = new NodeList();
            AttributeNode node = new AttributeNode(tagNode);
            nodes.Add(node);
            nodes.Add(tagNode);

            int offset = 2;
            AttributeNode myNode = (AttributeNode)node.Parse(nodes, tagNode, line, ref offset);

            bool inStr = true;
            string res = myNode.ToCode(ref inStr, false);

            //Assert.Equal("\"this\"+testCase.Id+\"id\"", myNode.GetAttribute("value").Value);
            //Assert.Equal("value=\"\"\"); sb.Append(\"this\"+testCase.Id+\"id\"); sb.Append(@\"\"\"", res);
        }

        
    }
}
