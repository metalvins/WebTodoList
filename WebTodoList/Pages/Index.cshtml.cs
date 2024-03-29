using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using WebTodoList.Authorization;
using WebTodoList.Data;
using WebTodoList.Models;

namespace WebTodoList.Pages
{
    [AllowAnonymous]
    public class IndexModel : TodoModel
    {
        private readonly WebTodoList.Data.ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context,
        IAuthorizationService authorizationService,
        UserManager<IdentityUser> userManager)
        : base(context, authorizationService, userManager) {
            _context = context;
        }

        public IList<TodoItem> TodoItem { get;set; }

        public async Task OnGetAsync()
        {
            var todoItems = from c in Context.Blogs
                           select c;

            var currentUserId = UserManager.GetUserId(User);

            // Only items owned by user will be shown

            todoItems = todoItems.Where(c => c.Email == currentUserId);
            TodoItem = await _context.Blogs.ToListAsync();
        }
        public async Task<ActionResult> OnPostAsync(int? id)
        {
            // Fetch Item from DB to get UserID.
            var todo = await _context
                .Blogs.AsNoTracking()
                .FirstOrDefaultAsync(m => m.ItemId == id);

            if (todo == null)
            {
                return NotFound();
            }

            var isAuthorized = await AuthorizationService.AuthorizeAsync(
                                                     User, todo,
                                                     TodoOperations.Complete);
            if (!isAuthorized.Succeeded)
            {
                return Forbid();
            }
            /*
            List<TodoItem> Items = new List<TodoItem>(TodoItem);
            TodoItem Item = Items.Find(x => x.ItemId == id);
            Item.ItemId = todo.ItemId;
            */
            todo.Done = TodoStatus.Completed;
            _context.Attach(todo).State = EntityState.Modified;

            /*
            if (TodoItem.Done == TodoStatus.NotCompleted)
            {
                // If task is edited after completion,
                // set it back to not completed.
                var canApprove = await AuthorizationService.AuthorizeAsync(User,
                                        TodoItem,
                                        TodoOperations.Complete);
                TodoItem.Done = TodoStatus.Completed;
            }
            */
            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TodoItemExists(todo.ItemId))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool TodoItemExists(int id)
        {
            return _context.Blogs.Any(e => e.ItemId == id);
        }
    }
}
